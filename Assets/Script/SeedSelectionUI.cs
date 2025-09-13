using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SeedSelectionUI : MonoBehaviour
{
    public static SeedSelectionUI Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] GameObject root;          // egész panel
    [SerializeField] Transform listContent;    // ide spawnoljuk a gombokat
    [SerializeField] Button seedButtonPrefab;  // Button prefab (rajta Image + 2 TMP text)
    [SerializeField] Button cancelButton;

    Action<PlantDefinition> onPicked;
    readonly List<GameObject> spawned = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Hide();
        if (cancelButton) cancelButton.onClick.AddListener(() => { onPicked = null; Hide(); });
    }

    public struct SeedOption
    {
        public PlantDefinition def;
        public int count;
        public Sprite icon;
    }

    public void Show(List<SeedOption> options, Action<PlantDefinition> onPicked)
    {
        if (root == null || listContent == null || seedButtonPrefab == null)
        {
            Debug.LogError("[SeedSelectionUI] Missing references on component.");
            return;
        }

        Clear();
        this.onPicked = onPicked;

        foreach (var opt in options)
        {
            var b = Instantiate(seedButtonPrefab, listContent);
            spawned.Add(b.gameObject);

            // Keresünk egy ikont és 1-2 TMP szöveget a gyerekek közt
            var img = b.GetComponentInChildren<Image>();
            if (img) img.sprite = opt.icon ? opt.icon : (opt.def ? opt.def.seedSprite : null);

            var tmps = b.GetComponentsInChildren<TextMeshProUGUI>();
            if (tmps.Length > 0) tmps[0].text = opt.def ? opt.def.displayName : "???";
            if (tmps.Length > 1) tmps[1].text = $"x{opt.count}";

            var localDef = opt.def;
            b.onClick.AddListener(() =>
            {
                var cb = this.onPicked; // elmentjük, mert Hide() kinullázhatná
                Hide();
                cb?.Invoke(localDef);
            });
        }

        root.SetActive(true);
        // opcionálisan: Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    public void Hide()
    {
        root.SetActive(false);
        Clear();
        onPicked = null;
    }

    void Clear()
    {
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i]) Destroy(spawned[i]);
        spawned.Clear();
    }
}
