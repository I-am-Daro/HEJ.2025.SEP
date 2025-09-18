using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class BuildManager : MonoBehaviour
{
    public static BuildManager I { get; private set; }

    [Header("Catalog")]
    public List<BuildingDefinition> catalog = new();

    [Header("Placement")]
    public float gridSize = 1f;
    public Color okColor = new(0f, 1f, 0f, 0.4f);
    public Color badColor = new(1f, 0f, 0f, 0.4f);
    public Key rotateKey = Key.R;

    [Header("Optional")]
    public string ghostLayerName = "Ghost";

    Camera cam;
    BuildingDefinition currentDef;
    GameObject ghost;
    SpriteRenderer[] ghostSprites;
    float rotZ;
    int ghostLayer = -1;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        if (!string.IsNullOrEmpty(ghostLayerName))
        {
            int id = LayerMask.NameToLayer(ghostLayerName);
            if (id >= 0) ghostLayer = id;
        }

        // Scene váltáskor újra keressük a kamerát
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    void OnDestroy()
    {
        if (I == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            I = null;
        }
    }


    void Start()
    {
        cam = Camera.main ? Camera.main : FindFirstObjectByType<Camera>();
        if (!cam) Debug.LogError("[BuildManager] No camera found.");
        RefreshCamera();
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m) => RefreshCamera();
    void OnActiveSceneChanged(Scene prev, Scene next) => RefreshCamera();

    void RefreshCamera()
    {
        // 1) próbáljuk a MainCamera taget
        cam = Camera.main;

        // 2) ha még sincs, keressünk bármilyen aktív kamerát
        if (cam == null)
        {
            var cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var c in cams)
            {
                if (c != null && c.isActiveAndEnabled) { cam = c; break; }
            }
        }

        // 3) ha még mindig nincs, várunk – Update-ben újra próbálunk
        if (cam == null)
            Debug.LogWarning("[BuildManager] No active camera found in scene. Ghost will stick at (0,0,0) until a camera appears.");
    }

    void Update()
    {
        if (currentDef == null || ghost == null) return;

        // scene-váltás után lehet, hogy még nincs kamera – próbáljuk újra
        if (cam == null || !cam.isActiveAndEnabled) RefreshCamera();
        if (cam == null) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector3 m = MouseWorld(mouse.position.ReadValue());
        m.z = 0f;
        m.x = Mathf.Round(m.x / gridSize) * gridSize;
        m.y = Mathf.Round(m.y / gridSize) * gridSize;
        ghost.transform.position = m;

        if (currentDef.canRotate && Keyboard.current != null && Keyboard.current[rotateKey].wasPressedThisFrame)
        {
            rotZ = (rotZ + 90f) % 360f;
            ghost.transform.rotation = Quaternion.Euler(0, 0, rotZ);
        }

        bool valid = IsValid(m, rotZ, currentDef);
        SetGhostColor(valid ? okColor : badColor);

        if (mouse.leftButton.wasPressedThisFrame && valid) Place(m, rotZ, currentDef);
        else if (mouse.rightButton.wasPressedThisFrame || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
            CancelPlacement();
    }

    Vector3 MouseWorld(Vector2 screenPos)
    {
        if (!cam) return Vector3.zero;
        var v = new Vector3(screenPos.x, screenPos.y, Mathf.Abs(cam.transform.position.z));
        return cam.ScreenToWorldPoint(v);
    }

    bool IsValid(Vector3 pos, float rot, BuildingDefinition def)
    {
        var hits = Physics2D.OverlapBoxAll(pos, def.size, rot, def.blockMask);
        return hits == null || hits.Length == 0;
    }

    void Place(Vector3 pos, float rot, BuildingDefinition def)
    {
        if (!def || !def.prefab) { Debug.LogError("Place: missing prefab"); return; }

        var go = Instantiate(def.prefab, pos, Quaternion.Euler(0, 0, rot));
        go.name = def.displayName;

        // Stabil ID – mindig ÚJ az instance-nek
        var sid = StableId.AddTo(go);
        sid.AssignNewRuntimeId();
        string id = sid.Id;

        // Mentés (a Te GameData-dba)
        GameData.I?.RegisterPlaced(id, def.id, pos, rot);

        // Hálózat: ha vannak node-ok, azonnal számoljuk újra
        var anyNode = go.GetComponentInChildren<PowerNode>(true);
        if (anyNode) PowerGrid.I?.Rebuild();
    }

    public void CancelPlacement()
    {
        currentDef = null;
        if (ghost) Destroy(ghost);
        ghost = null;
        ghostSprites = null;
        rotZ = 0f;

        // takarítás / friss áramháló újraszámolása
        PowerGrid.I?.Rebuild();
    }

    public void OpenBuildMenu()
    {
        if (BuildMenuUI.I == null) { Debug.LogError("BuildMenuUI missing"); return; }
        BuildMenuUI.I.Show(catalog);
    }

    public void Pick(BuildingDefinition def)
    {
        currentDef = def;
        MakeGhost(def);
        Debug.Log($"[BuildManager] Pick: {def.displayName}");
    }

    void MakeGhost(BuildingDefinition def)
    {
        if (!def || !def.prefab) return;

        if (ghost) Destroy(ghost);
        ghost = Instantiate(def.prefab);
        ghost.name = $"GHOST_{def.displayName}";
        rotZ = 0f;

        // JELÖLÉS: ez egy ghost példány
        ghost.AddComponent<GhostMarker>();

        if (ghostLayer >= 0) SetLayerRecursive(ghost, ghostLayer);

        foreach (var col in ghost.GetComponentsInChildren<Collider2D>(true)) col.enabled = false;
        foreach (var rb in ghost.GetComponentsInChildren<Rigidbody2D>(true))
        { rb.simulated = false; rb.isKinematic = true; rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; }
        foreach (var mb in ghost.GetComponentsInChildren<MonoBehaviour>(true)) mb.enabled = false;

        ghostSprites = ghost.GetComponentsInChildren<SpriteRenderer>(true);
        if (ghostSprites == null || ghostSprites.Length == 0)
            Debug.LogWarning("[BuildManager] Ghost has no SpriteRenderer (might be invisible).");

        SetGhostColor(okColor);
    }


    void SetGhostColor(Color c)
    {
        if (ghostSprites == null) return;
        foreach (var sr in ghostSprites) if (sr) sr.color = c;
    }

    void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
    }
}
