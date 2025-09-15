using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildMenuUI : MonoBehaviour
{
    public static BuildMenuUI I { get; private set; }

    [SerializeField] GameObject root;       // panel gyökér
    [SerializeField] Transform listParent;  // ScrollView Content
    [SerializeField] Button itemButtonPrefab;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        Hide();
    }

    public void Show(List<BuildingDefinition> defs)
    {
        Clear();

        foreach (var d in defs)
        {
            var b = Instantiate(itemButtonPrefab, listParent);

            var img = b.GetComponentInChildren<Image>();
            if (img) img.sprite = d.icon;

            var label = b.GetComponentInChildren<TextMeshProUGUI>();
            if (label) label.text = d.displayName;

            b.onClick.AddListener(() => {
                Hide();
                if (BuildManager.I == null) { Debug.LogError("BuildManager not found"); return; }
                BuildManager.I.Pick(d);     // <- ezt hívjuk
            });
        }

        root.SetActive(true);
    }

    public void Hide()
    {
        root.SetActive(false);
        Clear();
    }

    void Clear()
    {
        for (int i = listParent.childCount - 1; i >= 0; --i)
            Destroy(listParent.GetChild(i).gameObject);
    }
}
