using System.Collections.Generic;
using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData I { get; private set; }

    [System.Serializable]
    public class BedSave
    {
        public bool hasPlant;
        public string plantDefId;
        public PlantStage stage;
        public int daysLeftInStage;
        public int lastWateredDay = int.MinValue; // melyik napon lett utoljára locsolva
    }

    // greenhouseId -> (bedId -> BedSave)
    Dictionary<string, Dictionary<string, BedSave>> beds =
        new Dictionary<string, Dictionary<string, BedSave>>();

    [Header("Plant registry (töltsd ki a PlantDefinition SO-kat)")]
    public PlantDefinition[] plantRegistry;

    Dictionary<string, PlantDefinition> reg;

    bool subscribedToDay = false;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        reg = new Dictionary<string, PlantDefinition>();
        if (plantRegistry != null)
            foreach (var p in plantRegistry)
                if (p && !string.IsNullOrEmpty(p.id))
                    reg[p.id] = p;

        TrySubscribeDay();   // <-- elsõ próbálkozás
    }

    void Update()
    {
        // ha az Awake-kor még nem volt DayNightSystem, itt pótoljuk a feliratkozást
        if (!subscribedToDay) TrySubscribeDay();
    }


    PlantDefinition DefById(string id)
        => (id != null && reg != null && reg.TryGetValue(id, out var d)) ? d : null;

    public BedSave GetOrCreateBed(string greenhouseId, string bedId)
    {
        if (string.IsNullOrEmpty(greenhouseId) || string.IsNullOrEmpty(bedId)) return null;

        if (!beds.TryGetValue(greenhouseId, out var dict))
        {
            dict = new Dictionary<string, BedSave>();
            beds[greenhouseId] = dict;
        }

        if (!dict.TryGetValue(bedId, out var save))
        {
            save = new BedSave
            {
                hasPlant = false,
                plantDefId = null,
                stage = PlantStage.Seed,
                daysLeftInStage = 0,
                lastWateredDay = int.MinValue
            };
            dict[bedId] = save;
        }

        return save;
    }

    public void MarkWatered(string greenhouseId, string bedId, int day)
    {
        var s = GetOrCreateBed(greenhouseId, bedId);
        if (s == null) return;
        s.lastWateredDay = day;
    }

    public bool IsWatered(string greenhouseId, string bedId, int day)
    {
        var s = GetOrCreateBed(greenhouseId, bedId);
        if (s == null) return false;
        return s.lastWateredDay == day;
    }

    void TrySubscribeDay()
    {
        if (DayNightSystem.Instance != null && !subscribedToDay)
        {
            DayNightSystem.Instance.OnDayAdvanced += OnDay;
            subscribedToDay = true;
            UnityEngine.Debug.Log("[GameData] Subscribed to DayNightSystem.");
        }
    }

    public void WritePlant(string greenhouseId, string bedId, PlantDefinition def, PlantStage stage, int daysLeft)
    {
        var s = GetOrCreateBed(greenhouseId, bedId);
        if (s == null) return;

        s.hasPlant = def != null;
        s.plantDefId = def ? def.id : null;
        s.stage = stage;
        s.daysLeftInStage = daysLeft;

        // ÚJ: ha ültetés (Seed állapot), töröljük a locsolás jelölést
        if (def != null && stage == PlantStage.Seed)
            s.lastWateredDay = int.MinValue;
    }

    void OnDay(int newDay)
    {
        UnityEngine.Debug.Log($"[GameData] Day tick {newDay}");
        foreach (var gh in beds.Values)
            foreach (var s in gh.Values)
            {
                if (!s.hasPlant) continue;
                if (s.stage == PlantStage.Fruiting || s.stage == PlantStage.Withered) continue;

                s.daysLeftInStage--;
                if (s.daysLeftInStage <= 0)
                {
                    var def = DefById(s.plantDefId);
                    if (def == null) continue;

                    switch (s.stage)
                    {
                        case PlantStage.Seed:
                            s.stage = PlantStage.Sapling;
                            s.daysLeftInStage = Mathf.Max(1, def.daysSeedToSapling);   // <-- helyes
                            break;
                        case PlantStage.Sapling:
                            s.stage = PlantStage.Mature;
                            s.daysLeftInStage = Mathf.Max(1, def.daysSaplingToMature);
                            break;
                        case PlantStage.Mature:
                            s.stage = PlantStage.Fruiting;
                            s.daysLeftInStage = 0;
                            break;
                    }
                }
            }
    }

    public PlantDefinition ResolveDef(string plantDefId) => DefById(plantDefId);
}
