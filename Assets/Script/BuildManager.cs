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

    [Header("Build rules")]
    [Tooltip("Ha be van kapcsolva: csak GravityBubble zónán belül lehet építkezni.")]
    public bool requireGravityBubble = true;

    [Tooltip("Csak az üvegházra érvényes tiltott sugár a hajó körül.")]
    public float greenhouseNoBuildRadius = 5f;

    [Tooltip("A hajót jelölő tag (a no-build kör meghatározásához).")]
    public string spaceshipTag = "Spaceship";

    Camera cam;
    BuildingDefinition currentDef;
    GameObject ghost;
    SpriteRenderer[] ghostSprites;
    float rotZ;
    int ghostLayer = -1;

    // <<< ÚJ: játékos inv cache az ár-ellenőrzéshez >>>
    PlayerInventory cachedInv;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        if (!string.IsNullOrEmpty(ghostLayerName))
        {
            int id = LayerMask.NameToLayer(ghostLayerName);
            if (id >= 0) ghostLayer = id;
        }

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
        RefreshInventoryCache();
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        RefreshCamera();
        RefreshInventoryCache();
    }

    void OnActiveSceneChanged(Scene prev, Scene next)
    {
        RefreshCamera();
        RefreshInventoryCache();
    }

    void RefreshCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            var cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var c in cams)
            {
                if (c != null && c.isActiveAndEnabled) { cam = c; break; }
            }
        }
        if (cam == null)
            Debug.LogWarning("[BuildManager] No active camera found in scene. Ghost will stick at (0,0,0) until a camera appears.");
    }

    void RefreshInventoryCache()
    {
        // egyetlen játékos van → elég egyszer becache-elni
        cachedInv = FindFirstObjectByType<PlayerInventory>();
    }

    void Update()
    {
        if (currentDef == null || ghost == null) return;

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

    bool HasEnoughIron(BuildingDefinition def)
    {
        if (def == null || def.ironCost <= 0) return true; // nincs költség
        if (!cachedInv) RefreshInventoryCache();
        return cachedInv != null && cachedInv.HasIron(def.ironCost);
    }

    bool IsValid(Vector3 pos, float rot, BuildingDefinition def)
    {
        // 0) GravityBubble szabály
        if (requireGravityBubble && !GravityBubble.AnyContains(pos))
            return false;

        // 0.5) Van-e elég Iron a játékosnál?
        if (!HasEnoughIron(def))
            return false;

        // 1) blokk ütközés
        var hits = Physics2D.OverlapBoxAll(pos, def.size, rot, def.blockMask);
        if (hits != null && hits.Length > 0) return false;

        // 2) def szerinti távolságszabály
        if (def.forbidNearTagged && !string.IsNullOrEmpty(def.nearTag) && def.minDistance > 0f)
        {
            var targets = GameObject.FindGameObjectsWithTag(def.nearTag);
            for (int i = 0; i < targets.Length; i++)
            {
                var t = targets[i];
                if (!t) continue;
                float d = Vector2.Distance(pos, t.transform.position);
                if (d < def.minDistance) return false;
            }
        }

        // 3) csak Greenhouse: no-build kör a hajó körül
        if (def != null && def.id == "Greenhouse" && greenhouseNoBuildRadius > 0.01f)
        {
            var ship = GameObject.FindGameObjectWithTag(spaceshipTag);
            if (ship != null)
            {
                float d = Vector2.Distance(pos, ship.transform.position);
                if (d < greenhouseNoBuildRadius) return false;
            }
        }

        return true;
    }

    void Place(Vector3 pos, float rot, BuildingDefinition def)
    {
        if (!def || !def.prefab) { Debug.LogError("Place: missing prefab"); return; }

        // Biztonsági ellenőrzés: költség levonása
        if (def.ironCost > 0)
        {
            if (!cachedInv) RefreshInventoryCache();
            if (cachedInv == null || !cachedInv.SpendIron(def.ironCost))
            {
                Debug.LogWarning($"[Build] Not enough Iron to place {def.displayName} (cost: {def.ironCost}).");
                return;
            }
        }

        var go = Instantiate(def.prefab, pos, Quaternion.Euler(0, 0, rot));
        go.name = def.displayName;

        var sid = StableId.AddTo(go);
        sid.AssignNewRuntimeId();
        string id = sid.Id;

        GameData.I?.RegisterPlaced(id, def.id, pos, rot);

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
