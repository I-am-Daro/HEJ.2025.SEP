using System;
using System.Collections.Generic;
using UnityEngine;

/// GameData – egységes mentés/állapotkezelő
/// - Üvegház ágyások és növények állapota
/// - Lerakott épületek nyilvántartása (exterior)
public class GameData : MonoBehaviour
{
    public static GameData I { get; private set; }

    // -----------------------------
    // PLANT SAVE STRUCTURE
    // -----------------------------
    [Serializable]
    public class BedSave
    {
        public bool hasPlant;
        public string plantDefId;
        public PlantStage stage;
        public int daysLeftInStage;

        // Locsolási állapot
        public int lastWateredDay = int.MinValue;  // utolsó nap, amikor locsolták
        public int missedWaterDays = 0;            // egymás utáni száraz napok

        // Hervadás előtti stádium (revive-hoz)
        public PlantStage? prevStageBeforeWither = null;
    }

    // greenhouseId -> (bedId -> BedSave)
    private readonly Dictionary<string, Dictionary<string, BedSave>> beds =
        new Dictionary<string, Dictionary<string, BedSave>>();

    [Header("Plant registry (PlantDefinition SO-k)")]
    public PlantDefinition[] plantRegistry;
    private readonly Dictionary<string, PlantDefinition> plantReg =
        new Dictionary<string, PlantDefinition>();

    // -----------------------------
    // BUILDING SAVE STRUCTURE
    // -----------------------------
    [Serializable]
    public class PlacedBuilding
    {
        public string id;     // StableId (konkrét példány)
        public string defId;  // BuildingDefinition.id (típus)
        public Vector3 pos;
        public float rotZ;
    }

    [Header("Building registry (BuildingDefinition SO-k)")]
    public BuildingDefinition[] buildingRegistry;
    private readonly Dictionary<string, BuildingDefinition> buildReg =
        new Dictionary<string, BuildingDefinition>();

    // Exteriorban lerakott épületek
    public readonly List<PlacedBuilding> exteriorBuildings = new List<PlacedBuilding>();

    // -----------------------------
    // DAY MIRROR
    // -----------------------------
    public int CurrentDayMirror { get; private set; } = 1;

    // -----------------------------
    // UNITY LIFECYCLE
    // -----------------------------
    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // növény registry feltöltés
        plantReg.Clear();
        if (plantRegistry != null)
        {
            foreach (var p in plantRegistry)
                if (p && !string.IsNullOrEmpty(p.id))
                    plantReg[p.id] = p;
        }

        // building registry feltöltés
        buildReg.Clear();
        if (buildingRegistry != null)
        {
            foreach (var b in buildingRegistry)
                if (b && !string.IsNullOrEmpty(b.id))
                    buildReg[b.id] = b;
        }

        // nap tükör + növekedési logika bekötése
        if (DayNightSystem.Instance != null)
        {
            CurrentDayMirror = DayNightSystem.Instance.CurrentDay;
            DayNightSystem.Instance.OnDayAdvanced += OnDayAdvanced;
        }
    }

    private void OnDestroy()
    {
        if (DayNightSystem.Instance != null)
            DayNightSystem.Instance.OnDayAdvanced -= OnDayAdvanced;

        if (I == this) I = null;
    }

    // -----------------------------
    // REGISTRY HELPERS
    // -----------------------------
    private PlantDefinition PlantDefById(string id)
        => (!string.IsNullOrEmpty(id) && plantReg.TryGetValue(id, out var d)) ? d : null;

    public PlantDefinition ResolveDef(string plantDefId) => PlantDefById(plantDefId);

    public BuildingDefinition BuildingDef(string id)
        => (!string.IsNullOrEmpty(id) && buildReg.TryGetValue(id, out var d)) ? d : null;

    // -----------------------------
    // BEDS / PLANTS API
    // -----------------------------
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
                lastWateredDay = int.MinValue,
                missedWaterDays = 0,
                prevStageBeforeWither = null
            };
            dict[bedId] = save;
        }

        return save;
    }

    /// Növény állapot kiírása mentésbe (fő változat)
    public void WritePlant(string greenhouseId, string bedId,
                           PlantDefinition def, PlantStage stage, int daysLeft)
    {
        var s = GetOrCreateBed(greenhouseId, bedId);
        if (s == null) return;

        s.hasPlant = def != null;
        s.plantDefId = def ? def.id : null;
        s.stage = stage;
        s.daysLeftInStage = daysLeft;

        // üres ágyás → locsolási és wither meta reset
        if (!s.hasPlant)
        {
            s.lastWateredDay = int.MinValue;
            s.missedWaterDays = 0;
            s.prevStageBeforeWither = null;
        }
    }

    /// Kompat overload – ha a régi kód még hívja wateredToday flaggel
    public void WritePlant(string greenhouseId, string bedId,
                           PlantDefinition def, PlantStage stage, int daysLeft, bool wateredToday)
    {
        WritePlant(greenhouseId, bedId, def, stage, daysLeft);
        if (wateredToday)
        {
            int d = DayNightSystem.Instance ? DayNightSystem.Instance.CurrentDay : CurrentDayMirror;
            MarkWatered(greenhouseId, bedId, d);
        }
    }

    /// Bejelöli, hogy az adott napon meg lett locsolva
    public void MarkWatered(string greenhouseId, string bedId, int day)
    {
        var s = GetOrCreateBed(greenhouseId, bedId);
        if (s == null) return;

        s.lastWateredDay = day;

        // Ha hervadt volt és még nem semmisült meg, azonnal visszaállítjuk
        if (s.hasPlant && s.stage == PlantStage.Withered && s.prevStageBeforeWither.HasValue)
        {
            s.stage = s.prevStageBeforeWither.Value;
            s.prevStageBeforeWither = null;
            s.missedWaterDays = 0;
        }
    }

    /// Az adott napon locsolva volt-e
    public bool IsWatered(string greenhouseId, string bedId, int day)
    {
        var s = GetOrCreateBed(greenhouseId, bedId);
        if (s == null) return false;
        return s.lastWateredDay == day;
    }

    /// UI-ból explicit revive (nem kötelező használni, a MarkWatered is visszahozza)
    public bool TryReviveIfWithered(string greenhouseId, string bedId, out PlantStage revivedStage)
    {
        revivedStage = PlantStage.Seed;
        var s = GetOrCreateBed(greenhouseId, bedId);
        if (s == null || !s.hasPlant) return false;
        if (s.stage != PlantStage.Withered || !s.prevStageBeforeWither.HasValue) return false;

        revivedStage = s.prevStageBeforeWither.Value;
        s.stage = revivedStage;
        s.prevStageBeforeWither = null;
        s.missedWaterDays = 0;
        return true;
    }

    // -----------------------------
    // DAY TICK – növekedés / hervadás / megsemmisülés
    // -----------------------------
    private void OnDayAdvanced(int newDay)
    {
        CurrentDayMirror = newDay;

        foreach (var gh in beds.Values)
        {
            foreach (var s in gh.Values)
            {
                if (!s.hasPlant) continue;

                bool wateredPrevDay = (s.lastWateredDay == newDay - 1);

                if (!wateredPrevDay)
                {
                    // Első száraz nap → Withered
                    if (s.stage != PlantStage.Withered)
                    {
                        s.prevStageBeforeWither ??= s.stage;
                        s.stage = PlantStage.Withered;
                        s.missedWaterDays = 1;
                    }
                    else
                    {
                        // Második egymást követő száraz nap → megsemmisül
                        s.missedWaterDays++;
                        if (s.missedWaterDays >= 2)
                        {
                            s.hasPlant = false;
                            s.plantDefId = null;
                            s.stage = PlantStage.Seed;
                            s.daysLeftInStage = 0;
                            s.prevStageBeforeWither = null;
                            s.lastWateredDay = int.MinValue;
                            s.missedWaterDays = 0;
                        }
                    }

                    // száraz napon nincs növekedés
                    continue;
                }

                // locsolva volt
                s.missedWaterDays = 0;
                if (s.stage == PlantStage.Withered) continue; // biztonság

                // növekedés csak locsolt napon
                if (s.daysLeftInStage > 0) s.daysLeftInStage--;
                if (s.daysLeftInStage <= 0)
                {
                    var def = PlantDefById(s.plantDefId);
                    if (def == null) continue;

                    switch (s.stage)
                    {
                        case PlantStage.Seed:
                            s.stage = PlantStage.Sapling;
                            s.daysLeftInStage = Mathf.Max(1, def.daysSaplingToMature);
                            break;
                        case PlantStage.Sapling:
                            s.stage = PlantStage.Mature;
                            s.daysLeftInStage = Mathf.Max(1, def.daysMatureToFruiting);
                            break;
                        case PlantStage.Mature:
                            s.stage = PlantStage.Fruiting;
                            s.daysLeftInStage = 0;
                            break;
                        case PlantStage.Fruiting:
                            // marad termő állapotban
                            break;
                    }
                }
            }
        }
    }

    // -----------------------------
    // BUILDINGS API
    // -----------------------------
    public void RegisterPlaced(string stableId, string defId, Vector3 pos, float rotZ)
    {
        if (string.IsNullOrEmpty(stableId) || string.IsNullOrEmpty(defId)) return;

        var e = exteriorBuildings.Find(p => p.id == stableId);
        if (e == null)
        {
            e = new PlacedBuilding { id = stableId, defId = defId, pos = pos, rotZ = rotZ };
            exteriorBuildings.Add(e);
        }
        else
        {
            e.defId = defId;
            e.pos = pos;
            e.rotZ = rotZ;
        }
    }

    public void RemovePlaced(string stableId)
    {
        if (string.IsNullOrEmpty(stableId)) return;
        exteriorBuildings.RemoveAll(p => p.id == stableId);
    }
}
