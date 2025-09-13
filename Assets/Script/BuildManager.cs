using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildManager : MonoBehaviour
{
    public static BuildManager I { get; private set; }

    [Header("Catalog")]
    public List<BuildingDefinition> catalog = new(); // töltsd fel: Pipe, Greenhouse, stb.

    [Header("Placement")]
    public float gridSize = 1f;
    public Color okColor = new(0f, 1f, 0f, 0.4f);
    public Color badColor = new(1f, 0f, 0f, 0.4f);
    public Key rotateKey = Key.R; // ÚJ Input System-es Key

    Camera cam;
    BuildingDefinition currentDef;
    GameObject ghost;
    SpriteRenderer[] ghostSprites;
    float rotZ;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    void Start()
    {
        cam = Camera.main;
        if (!cam)
        {
            cam = FindFirstObjectByType<Camera>();
            if (!cam) Debug.LogError("[BuildManager] Nincs kamera a jelenetben (Camera.main is null).");
        }
    }

    void Update()
    {
        if (currentDef == null) return;

        // Ha valamiért nincs ghost, állítsuk elő újra (robosztusabb)
        if (!ghost)
        {
            Debug.Log("[BuildManager] Ghost hiányzik, újragyártás.");
            MakeGhost(currentDef);
            if (!ghost) return; // prefab gond esetén nem erőltetjük tovább
        }

        // Egér poz → világ
        if (cam == null) cam = Camera.main;
        var mouse = Mouse.current;
        if (mouse == null)
        {
            // ha gamepad-only, ide tehetsz egy másik poz forrást (pl. player előtti cella)
            return;
        }

        Vector3 m = MouseWorld(mouse.position.ReadValue());
        m.z = 0f;

        // snap rácsra
        m.x = Mathf.Round(m.x / gridSize) * gridSize;
        m.y = Mathf.Round(m.y / gridSize) * gridSize;

        ghost.transform.position = m;

        // forgatás
        if (currentDef.canRotate && Keyboard.current != null && Keyboard.current[rotateKey].wasPressedThisFrame)
        {
            rotZ = (rotZ + 90f) % 360f;
            ghost.transform.rotation = Quaternion.Euler(0, 0, rotZ);
        }

        bool valid = IsValid(m, rotZ, currentDef);
        SetGhostColor(valid ? okColor : badColor);

        // lerakás / megszakítás
        if (mouse.leftButton.wasPressedThisFrame && valid)
        {
            Place(m, rotZ, currentDef);
        }
        else if (mouse.rightButton.wasPressedThisFrame || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            CancelPlacement();
        }
    }

    Vector3 MouseWorld(Vector2 screenPos)
    {
        if (!cam) return Vector3.zero;
        // 2D ortho kameránál a Z-t a kamera Z-je adja, a world Z-t utólag 0-ra állítjuk
        var v = new Vector3(screenPos.x, screenPos.y, Mathf.Abs(cam.transform.position.z));
        return cam.ScreenToWorldPoint(v);
    }

    void SetGhostColor(Color c)
    {
        if (ghostSprites == null) return;
        foreach (var sr in ghostSprites)
        {
            if (!sr) continue;
            var col = sr.color;
            col = c;
            sr.color = col;
            // biztos, ami biztos: a SortingLayer maradjon látható
            if (string.IsNullOrEmpty(sr.sortingLayerName)) sr.sortingLayerID = 0; // Default
        }
    }

    bool IsValid(Vector3 pos, float rot, BuildingDefinition def)
    {
        if (def == null) return false;
        // egyszerű ütközésvizsgálat
        var hits = Physics2D.OverlapBoxAll(pos, def.size, rot, def.blockMask);
        if (hits != null && hits.Length > 0) return false;

        return true;
    }

    void Place(Vector3 pos, float rot, BuildingDefinition def)
    {
        if (def == null || def.prefab == null)
        {
            Debug.LogError("[BuildManager] Place: hiányzik a prefab a BuildingDefinitionból.");
            return;
        }

        var go = Instantiate(def.prefab, pos, Quaternion.Euler(0, 0, rot));
        go.name = def.displayName;

        // Hálózat újraszámolás
        PowerGrid.I?.Rebuild();

        // pipe-ot tipikusan sorban többet is raksz → maradjon aktív a mód
        // ha inkább kilépjünk, hívd meg itt a CancelPlacement();-et
    }

    public void CancelPlacement()
    {
        currentDef = null;
        if (ghost) Destroy(ghost);
        ghost = null;
        ghostSprites = null;
        rotZ = 0f;
    }

    public void OpenBuildMenu()
    {
        if (BuildMenuUI.I == null)
        {
            Debug.LogError("[BuildManager] BuildMenuUI nincs a jelenetben.");
            return;
        }
        BuildMenuUI.I.Show(catalog, Pick);
    }

    void Pick(BuildingDefinition def)
    {
        if (def == null)
        {
            Debug.LogWarning("[BuildManager] Pick: null def");
            return;
        }
        currentDef = def;
        MakeGhost(def);
        Debug.Log($"[BuildManager] Pick: {def.displayName}");
    }

    void MakeGhost(BuildingDefinition def)
    {
        if (!def || !def.prefab)
        {
            Debug.LogError("[BuildManager] MakeGhost: hiányzik a prefab a def-ben.");
            return;
        }

        if (ghost) Destroy(ghost);
        ghost = Instantiate(def.prefab);
        ghost.name = $"GHOST_{def.displayName}";
        rotZ = 0f;

        // Renderer gyűjtés és áttetszővé tétel
        ghostSprites = ghost.GetComponentsInChildren<SpriteRenderer>(true);
        if (ghostSprites == null || ghostSprites.Length == 0)
            Debug.LogWarning("[BuildManager] Ghost prefabben nincs SpriteRenderer – lehet, hogy nem fogsz látni semmit.");

        SetGhostColor(okColor);

        // Kapcsold ki a collider-eket, interakciós scripteket a ghostban
        foreach (var col in ghost.GetComponentsInChildren<Collider2D>(true))
            col.enabled = false;

        // (Opcionális) Kapcsold ki a többi MonoBehaviour-t, ami játéklogikát futtatna
        foreach (var mb in ghost.GetComponentsInChildren<MonoBehaviour>(true))
        {
            // Ezek maradhatnak, ha csak adatot hordoznak – de a ghostnak nincs rájuk szüksége
            if (mb is SpriteRenderer) continue;
            if (mb is Transform) continue;
            // Hálózati komponensek sem kellenek a ghosthoz
            if (mb is PowerNode || mb is PowerConsumer) { /* maradhatnak, de nem muszáj */ }

            // Ha biztosan nem kell logika a ghoston:
            // if (!(mb is SpriteRenderer)) mb.enabled = false;
        }
    }
}
