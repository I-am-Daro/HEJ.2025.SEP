using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnalyzerUI : MonoBehaviour
{
    public static AnalyzerUI I { get; private set; }

    [Header("Panel")]
    [SerializeField] GameObject root;                 // panel gyökér
    [SerializeField] Button closeButton;

    [Header("List")]
    [SerializeField] Transform listParent;            // ScrollView Content
    [SerializeField] Button itemButtonPrefab;         // listaelem (Button + Text)

    [Header("Header")]
    [SerializeField] TextMeshProUGUI statusText;      // "Idle" / "Analyzing..."
    [SerializeField] Button startButton;              // indítás kiválasztott mintával

    [Header("Extra controls")]
    [SerializeField] Button resultsButton;            // "Results" gomb (opcionális)

    [Header("Refs (optional, auto-find if null)")]
    [SerializeField] AnalyzerStation stationRef;
    [SerializeField] PlayerInventory inventoryRef;
    [SerializeField] string stationTag = "AnalyzerStation";

    [SerializeField] AnalyzerResultsPanel resultsPanel; // ResultRoot-on levő script

    // runtime
    RockSampleDefinition selected;
    bool subscribed;

    // -------- Lifecycle --------
    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        if (!root) root = gameObject; // fallback
        root.SetActive(false);

        // UI gombok bekötése
        if (startButton) startButton.onClick.AddListener(OnStartClicked);
        if (closeButton) closeButton.onClick.AddListener(Hide);
        if (resultsButton) resultsButton.onClick.AddListener(OnResultsButtonClicked);

        EnsureRefs();
    }

    void OnEnable()
    {
        EnsureRefs();
        SubscribeStation();
        RefreshHeader();
    }

    void OnDisable()
    {
        UnsubscribeStation();
    }

    void OnDestroy()
    {
        UnsubscribeStation();
        if (I == this) I = null;
    }

    // -------- Public API --------
    public void Show()
    {
        EnsureRefs();
        SubscribeStation();

        root.SetActive(true);
        RebuildList();
        RefreshHeader();
        TryOpenResultsPanelIfAny();   // ha van pending result, automatikusan felugrik
    }

    public void Hide()
    {
        root.SetActive(false);
        ClearList();
        selected = null;
    }

    // -------- Internals --------
    void EnsureRefs()
    {
        if (!inventoryRef)
            inventoryRef = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Exclude);

        if (!stationRef)
        {
            // 1) tag alapján
            if (!string.IsNullOrEmpty(stationTag))
            {
                var go = GameObject.FindGameObjectWithTag(stationTag);
                if (go) stationRef = go.GetComponent<AnalyzerStation>();
            }
            // 2) típus alapján
            if (!stationRef)
                stationRef = FindFirstObjectByType<AnalyzerStation>(FindObjectsInactive.Exclude);
        }

        // resultsPanel auto-find, ha nem adtad meg kézzel
        if (!resultsPanel)
            resultsPanel = GetComponentInChildren<AnalyzerResultsPanel>(true);
    }

    void SubscribeStation()
    {
        if (subscribed || !stationRef) return;
        stationRef.OnJobStarted += OnJobStarted;
        stationRef.OnDayTick += OnDayTick;
        stationRef.OnJobCompleted += OnJobCompleted;
        stationRef.OnResultsReady += OnResultsReady;
        stationRef.OnResultsAccepted += OnResultsAccepted;
        subscribed = true;
    }

    void UnsubscribeStation()
    {
        if (!subscribed || !stationRef) return;
        stationRef.OnJobStarted -= OnJobStarted;
        stationRef.OnDayTick -= OnDayTick;
        stationRef.OnJobCompleted -= OnJobCompleted;
        stationRef.OnResultsReady -= OnResultsReady;
        stationRef.OnResultsAccepted -= OnResultsAccepted;
        subscribed = false;
    }

    // -------- List building --------
    void RebuildList()
    {
        ClearList();
        if (!inventoryRef || itemButtonPrefab == null || listParent == null) return;

        foreach (var kv in inventoryRef.EnumerateRockSamples())
        {
            var def = kv.Key;
            int count = kv.Value;
            if (!def || count <= 0) continue;

            var btn = Instantiate(itemButtonPrefab, listParent);
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label) label.text = $"{def.displayName}  ×{count}";

            RockSampleDefinition captured = def; // capture helyesen
            btn.onClick.AddListener(() =>
            {
                selected = captured;
                HighlightSelection(btn);
                RefreshHeader();
            });
        }

        if (selected == null) RefreshHeader();
    }

    void ClearList()
    {
        if (!listParent) return;
        for (int i = listParent.childCount - 1; i >= 0; --i)
            Destroy(listParent.GetChild(i).gameObject);
    }

    void HighlightSelection(Button chosen)
    {
        if (!listParent) return;
        for (int i = 0; i < listParent.childCount; i++)
        {
            var b = listParent.GetChild(i).GetComponent<Button>();
            if (b) b.interactable = (b != chosen);
        }
    }

    // -------- Header / Buttons --------
    void RefreshHeader()
    {
        bool hasStation = stationRef != null;
        bool hasJob = hasStation && stationRef.HasActiveJob;
        bool resultsReady = hasStation && stationRef.PeekPendingResults() != null;

        if (resultsReady)
        {
            if (statusText) statusText.text = "Results ready – open Analyzer to accept.";
        }
        else if (hasJob)
        {
            string name = stationRef.CurrentSample ? stationRef.CurrentSample.displayName : "Unknown";
            if (statusText) statusText.text = $"Analyzing: {name}  ({stationRef.DaysLeft} day(s) left)";
        }
        else
        {
            if (statusText) statusText.text = selected
                ? $"Selected: {selected.displayName}"
                : "Idle – select a sample to analyze";
        }

        if (startButton)
        {
            bool canStart = !resultsReady && !hasJob && selected != null &&
                            inventoryRef && inventoryRef.GetRockSampleCount(selected) > 0;
            startButton.interactable = canStart;
        }

        if (resultsButton) // csak akkor aktív, ha van elfogadásra váró eredmény
            resultsButton.interactable = resultsReady;
    }

    // -------- Buttons --------
    void OnStartClicked()
    {
        if (!stationRef || !inventoryRef || selected == null) return;
        if (stationRef.HasActiveJob || stationRef.PeekPendingResults() != null)
        {
            RefreshHeader();
            return;
        }

        bool ok = stationRef.TryStartWith(selected, inventoryRef);
        if (!ok) RebuildList();
        else { selected = null; RebuildList(); }
        RefreshHeader();
    }

    void OnResultsButtonClicked()
    {
        TryOpenResultsPanelIfAny();
    }

    void TryOpenResultsPanelIfAny()
    {
        if (!resultsPanel || !stationRef) return;
        var res = stationRef.PeekPendingResults();
        if (res != null)
            resultsPanel.Show(stationRef, res);
    }

    // -------- Station events --------
    void OnJobStarted(RockSampleDefinition def, int days) { RefreshHeader(); }
    void OnDayTick(RockSampleDefinition def, int daysLeft) { RefreshHeader(); }
    void OnJobCompleted(RockSampleDefinition def) { RefreshHeader(); }
    void OnResultsReady(AnalyzerStation.AnalysisResults res)
    {
        // Ha a panel nyitva van, azonnal felugrik a Result
        if (root != null && root.activeInHierarchy) TryOpenResultsPanelIfAny();
        RefreshHeader();
    }
    void OnResultsAccepted()
    {
        RebuildList();
        RefreshHeader();
    }
}
