using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // Keyboard.current?.escapeKey...
#endif

public class BuildMenuUI : MonoBehaviour
{
    public static BuildMenuUI I { get; private set; }

    [SerializeField] GameObject root;
    [SerializeField] Transform listParent;
    [SerializeField] Button itemButtonPrefab;

    // --- új: idő megfagyasztás kezelése ---
    bool isOpen;
    float prevTimeScale = 1f;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        Hide();
    }

    void Update()
    {
        if (!isOpen) return;

        // ESC – régi és új input system támogatás
        bool esc =
#if ENABLE_INPUT_SYSTEM
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
#endif
            Input.GetKeyDown(KeyCode.Escape);

        if (esc) Hide();
    }

    public void Show(List<BuildingDefinition> defs)
    {
        // csak Exteriorban engedélyezett
        if (SceneMovementBoot.CurrentSceneKind != SceneKind.Exterior)
        {
            Debug.Log("[BuildMenuUI] Building is only allowed in Exterior scenes.");
            Hide();
            return;
        }

        Clear();

        foreach (var d in defs)
        {
            var b = Instantiate(itemButtonPrefab, listParent);

            var img = b.GetComponentInChildren<Image>();
            if (img) img.sprite = d.icon;

            var label = b.GetComponentInChildren<TextMeshProUGUI>();
            if (label)
                label.text = d.ironCost > 0
                    ? $"{d.displayName}  ({d.ironCost} Iron)"
                    : d.displayName;

            b.onClick.AddListener(() =>
            {
                // választás → menü bezár + idő vissza,
                // majd átadjuk a választást a BuildManagernek
                Hide();
                if (BuildManager.I == null) { Debug.LogError("BuildManager not found"); return; }
                BuildManager.I.Pick(d);
            });
        }

        if (root) root.SetActive(true);

        // idő fagyasztása
        if (!isOpen)
        {
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            isOpen = true;
        }
    }

    public void Hide()
    {
        if (root) root.SetActive(false);
        Clear();

        if (isOpen)
        {
            // idő visszaállítása arra, ami a megnyitás előtt volt
            Time.timeScale = prevTimeScale;
            isOpen = false;
        }
    }

    void OnDisable()
    {
        // ha valami miatt kikapcsol a GO miközben nyitva volt, állítsuk vissza az időt
        if (isOpen)
        {
            Time.timeScale = prevTimeScale;
            isOpen = false;
        }
    }

    void OnDestroy()
    {
        if (isOpen)
        {
            Time.timeScale = prevTimeScale;
            isOpen = false;
        }
    }

    void Clear()
    {
        if (!listParent) return;
        for (int i = listParent.childCount - 1; i >= 0; --i)
            Destroy(listParent.GetChild(i).gameObject);
    }
}
