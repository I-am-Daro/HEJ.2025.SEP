using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using UnityEngine;
using UnityEngine.Audio; // <<< ÚJ
using Random = UnityEngine.Random;

[RequireComponent(typeof(Collider2D))]
public class AnalyzerStation : MonoBehaviour, IInteractable
{
    [Header("State (runtime)")]
    [SerializeField] private RockSampleDefinition currentSample;
    [SerializeField] private int daysLeft;

    [Header("Optional SFX (clips)")]
    public AudioClip startAnalysisSfx;
    public AudioClip completeSfx;

    [Header("Audio Routing")]
    [Tooltip("Irányítsd erre a mixer ágra (SFX), hogy az Options/Audio csúszkák hassanak rá.")]
    public AudioMixerGroup sfxBus;               // <<< ÚJ: ide kösd a Mixer SFX Groupot
    [Range(0f, 1f)] public float sfxVolume = 1f; // opcionális helyi szorzó (Mixer mellett)

    // Belső egyshot forrás – hogy a mixer csoporton menjen át
    AudioSource oneShotSource;                   // <<< ÚJ

    // ==== ESEMÉNYEK UI/Observereknek ====
    public event Action<RockSampleDefinition, int> OnJobStarted;
    public event Action<RockSampleDefinition, int> OnDayTick;
    public event Action<RockSampleDefinition> OnJobCompleted; // lefutott az elemzés
    public event Action<AnalysisResults> OnResultsReady;      // van elfogadásra váró eredmény
    public event Action OnResultsAccepted;                    // játékos elfogadta

    // ==== Publikus olvasók ====
    public bool HasActiveJob => currentSample != null;
    public RockSampleDefinition CurrentSample => currentSample;
    public int DaysLeft => daysLeft;

    [Serializable]
    public class AnalysisResults
    {
        [Serializable]
        public struct ResourceGrant { public ResourceType type; public int amount; }

        public RockSampleDefinition source;
        public List<ResourceGrant> resources = new List<ResourceGrant>();
        public PlantDefinition seedDef;
        public int seedAmount;
    }

    AnalysisResults pendingResults;
    public bool HasPendingResults => pendingResults != null;
    public AnalysisResults PeekPendingResults() => pendingResults;

    string stableId;

    void Awake()
    {
        var sid = GetComponent<StableId>();
        if (!sid) sid = StableId.AddTo(gameObject);
        if (string.IsNullOrEmpty(sid.Id)) sid.AssignNewRuntimeId();
        stableId = sid.Id;

        // --- Belső SFX forrás setup (Mixer SFX csoporthoz kötve) ---
        oneShotSource = gameObject.AddComponent<AudioSource>();
        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.spatialBlend = 0f; // 2D
        if (sfxBus) oneShotSource.outputAudioMixerGroup = sfxBus;
        oneShotSource.volume = sfxVolume; // helyi szorzó (Mixer mellett)
    }

    void Reset()
    {
        var c = GetComponent<Collider2D>();
        c.isTrigger = true;
    }

    void OnEnable()
    {
        if (DayNightSystem.Instance != null)
            DayNightSystem.Instance.OnDayAdvanced += OnDayAdvanced;

        // Betöltés mentésből (ha volt futó job)
        if (!HasActiveJob && GameData.I != null && !string.IsNullOrEmpty(stableId))
        {
            if (GameData.I.TryGetAnalyzerJobRaw(stableId, out var savedId, out var savedDays))
            {
                var def = GameData.I.ResolveRock(savedId);
                if (def != null)
                {
                    currentSample = def;
                    daysLeft = savedDays;
                    OnJobStarted?.Invoke(currentSample, daysLeft);
                }
                else
                {
                    Debug.LogWarning($"[Analyzer] Mentés ok, de Rock ID ('{savedId}') nem oldható fel.");
                }
            }
        }
    }

    void OnDisable()
    {
        if (DayNightSystem.Instance != null)
            DayNightSystem.Instance.OnDayAdvanced -= OnDayAdvanced;

        if (HasActiveJob && GameData.I != null)
            GameData.I.WriteAnalyzerJob(stableId, currentSample, daysLeft);
    }

    void Update()
    {
        // Ha futás közben állítod inspectorban a sfxVolume-ot, érvényesüljön:
        if (oneShotSource) oneShotSource.volume = sfxVolume;
    }

    public string GetPrompt()
    {
        if (pendingResults != null) return "Open Analyzer (Results) (E)";
        if (!HasActiveJob) return "Open Analyzer (E)";
        return $"Analyzing: {currentSample.displayName} ({daysLeft} day(s) left)";
    }

    public void Interact(PlayerStats player)
    {
        if (!player) return;

        if (AnalyzerUI.I != null) AnalyzerUI.I.Show();
        else Debug.LogWarning("[Analyzer] No AnalyzerUI in scene. Place the AnalyzerUI prefab under Canvas.");
    }

    public bool TryStartWith(RockSampleDefinition def, PlayerInventory inv)
    {
        if (!def || !inv || HasActiveJob || pendingResults != null) return false;
        if (!inv.ConsumeRockSample(def, 1)) return false;

        StartAnalysis(def);

        // --- SFX: START (Mixer SFX ágon) ---
        PlaySfx(startAnalysisSfx);

        OnJobStarted?.Invoke(currentSample, daysLeft);

        if (GameData.I != null) GameData.I.WriteAnalyzerJob(stableId, currentSample, daysLeft);
        return true;
    }

    void StartAnalysis(RockSampleDefinition def)
    {
        currentSample = def;

        int dMin = Mathf.Max(0, def.analysisDaysMin);
        int dMax = Mathf.Max(dMin, def.analysisDaysMax);
        daysLeft = Random.Range(dMin, dMax + 1);

        if (daysLeft <= 0) CompleteAnalysis();
        else Debug.Log($"[Analyzer] Started: {def.displayName}, days: {daysLeft}");
    }

    void OnDayAdvanced(int newDay)
    {
        if (!HasActiveJob) return;

        if (daysLeft > 0) daysLeft--;
        OnDayTick?.Invoke(currentSample, daysLeft);

        if (GameData.I != null) GameData.I.WriteAnalyzerJob(stableId, currentSample, daysLeft);

        if (daysLeft <= 0) CompleteAnalysis();
    }

    void CompleteAnalysis()
    {
        if (!HasActiveJob) return;

        var results = new AnalysisResults { source = currentSample };

        if (currentSample.yields != null)
        {
            foreach (var y in currentSample.yields)
            {
                int amt = Random.Range(y.min, y.max + 1);
                if (amt > 0)
                    results.resources.Add(new AnalysisResults.ResourceGrant { type = y.type, amount = amt });
            }
        }

        if (currentSample.seedDef && currentSample.seedChancePercent > 0)
        {
            if (Random.Range(0, 100) < currentSample.seedChancePercent)
            {
                results.seedDef = currentSample.seedDef;
                results.seedAmount = Random.Range(Mathf.Max(1, currentSample.seedMin),
                                                  Mathf.Max(1, currentSample.seedMax) + 1);
            }
        }

        // --- SFX: COMPLETE (Mixer SFX ágon) ---
        PlaySfx(completeSfx);

        var finished = currentSample;

        if (GameData.I != null) GameData.I.ClearAnalyzerJob(stableId);

        currentSample = null;
        daysLeft = 0;

        pendingResults = results;

        OnJobCompleted?.Invoke(finished);
        OnResultsReady?.Invoke(pendingResults);
    }

    // alias a results panelhez
    public void AcceptPendingResults(PlayerInventory inv) => AcceptResults(inv);

    public void AcceptResults(PlayerInventory inv)
    {
        if (pendingResults == null) return;

        // Keresünk PlayerStats-ot (egyjátékosnál elég ez)
        var stats = FindFirstObjectByType<PlayerStats>(FindObjectsInactive.Exclude);

        if (pendingResults.resources != null)
        {
            foreach (var g in pendingResults.resources)
            {
                if (g.amount <= 0) continue;

                if (g.type == ResourceType.Water && stats != null)
                {
                    float before = stats.water;
                    stats.water = Mathf.Clamp(stats.water + g.amount, 0f, 100f);
                    Debug.Log($"[Analyzer] Water +{g.amount}  ({before} -> {stats.water})");
                }
                else
                {
                    inv?.AddResource(g.type, g.amount);
                }
            }
        }

        if (pendingResults.seedDef && pendingResults.seedAmount > 0)
            inv?.AddSeed(pendingResults.seedDef, pendingResults.seedAmount);

        pendingResults = null;
        OnResultsAccepted?.Invoke();
    }


    // ---------- Audio helper ----------
    void PlaySfx(AudioClip clip)
    {
        if (!clip || !oneShotSource) return;

        // biztosítsuk, hogy a mixer group mindig jó legyen (ha futás közben állítod be)
        if (sfxBus && oneShotSource.outputAudioMixerGroup != sfxBus)
            oneShotSource.outputAudioMixerGroup = sfxBus;

        oneShotSource.pitch = 1f;
        oneShotSource.PlayOneShot(clip, sfxVolume); // végső szorzó – a fő hangerőt a Mixer adja
    }
}
