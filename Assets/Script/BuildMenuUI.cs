using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildMenuUI : MonoBehaviour
{
    public static BuildMenuUI I { get; private set; }

    [SerializeField] GameObject root;
    [SerializeField] Transform listParent;
    [SerializeField] Button itemButtonPrefab;

    Action<BuildingDefinition> onPick;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        Hide();
    }

    public void Show(List<BuildingDefinition> defs, Action<BuildingDefinition> pick)
    {
        Clear();
        onPick = pick;

        foreach (var d in defs)
        {
            var b = Instantiate(itemButtonPrefab, listParent);
            var img = b.GetComponentInChildren<Image>();
            var tmps = b.GetComponentsInChildren<TextMeshProUGUI>();
            if (img) img.sprite = d.icon;
            if (tmps.Length > 0) tmps[0].text = d.displayName;

            b.onClick.AddListener(() => { Hide(); onPick?.Invoke(d); });
        }

        root.SetActive(true);
    }

    public void Hide()
    {
        root.SetActive(false);
        Clear();
        onPick = null;
    }

    void Clear()
    {
        for (int i = listParent.childCount - 1; i >= 0; --i)
            Destroy(listParent.GetChild(i).gameObject);
    }
}
