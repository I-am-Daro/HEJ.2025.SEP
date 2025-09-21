using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnalyzerResultsPanel : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] GameObject root;                 // ResultRoot (inaktív by default)
    [SerializeField] TextMeshProUGUI title;           // ResultRoot/Title
    [SerializeField] Transform listParent;            // ResultRoot/ResultListParent/Viewport/Content
    [SerializeField] TextMeshProUGUI rowPrefab;       // egy TMP sor (pl. "Iron +3")
    [SerializeField] Button acceptButton;             // ResultRoot/Accept_button

    // runtime
    AnalyzerStation boundStation;
    AnalyzerStation.AnalysisResults pending;

    void Awake()
    {
        if (!root) root = gameObject;
        if (root.activeSelf) root.SetActive(false);   // induláskor rejtve
    }

    public void Show(AnalyzerStation station, AnalyzerStation.AnalysisResults res)
    {
        boundStation = station;
        pending = res;

        if (!root) root = gameObject;
        root.SetActive(true);

        // Cím
        if (title)
        {
            string n = (res != null && res.source) ? res.source.displayName : "Unknown sample";
            title.text = $"Results: {n}";
        }

        // Lista feltöltés
        ClearList();

        if (res != null)
        {
            // resource sorok
            if (res.resources != null)
            {
                foreach (var g in res.resources)
                {
                    if (g.amount <= 0) continue;
                    AddRow($"{g.type}  +{g.amount}");
                }
            }

            // seed sor (ha volt)
            if (res.seedDef && res.seedAmount > 0)
            {
                AddRow($"{res.seedDef.displayName} seeds  +{res.seedAmount}");
            }
        }

        // Accept gomb
        if (acceptButton)
        {
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(OnAccept);
        }
    }

    void AddRow(string text)
    {
        if (!rowPrefab || !listParent) return;
        var row = Instantiate(rowPrefab, listParent);
        row.gameObject.SetActive(true);
        row.text = text;
    }

    void ClearList()
    {
        if (!listParent) return;
        for (int i = listParent.childCount - 1; i >= 0; --i)
        {
            Destroy(listParent.GetChild(i).gameObject);
        }
    }

    void OnAccept()
    {
        if (boundStation != null && pending != null)
        {
            // A Station végzi a jóváírást
            var inv = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Exclude);
            boundStation.AcceptResults(inv);
        }
        pending = null;
        root.SetActive(false);
    }
}
