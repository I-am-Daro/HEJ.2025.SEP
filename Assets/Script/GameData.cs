using System;
using System.Collections.Generic;
using UnityEngine;

/// Összevont GameData:
/// - Ágyások/növények állapota (greenhouseId -> bedId -> BedSave)
/// - Épületek (Exterior) mentése / visszatöltése
public class GameData : MonoBehaviour
{
    public static GameData I { get; private set; }

    // ========= PLANT REGISTRY / SAVE =========

    [Serializable]
    public class BedSave
    {
        public bool hasPlant;
        public string plantDefId;
        public PlantStage stage;
        public int daysLeftInStage;

        // víz status: mikor lett locsolva utoljára (játékbeli nap száma)
        public int lastWateredDay = int.MinValue;
    }

    // greenhouseId -> (bedId -> BedSave)
    Dictionary<string, Dictionary<string, BedSave>> beds =
        new Dictionary<string, Dictionary<string, BedSave>>();

    [Header("Plant registry (PlantDefinition SO-kat töltsd ide)")]
    public PlantDefinition[] plantRegistry;
    Dictionary<string, PlantDefinition> plantReg = new Dictionary<string, PlantDefinition>();

    // ========= BUILDING REGISTRY / SAVE =========

    [Serializable]
    public class PlacedBuilding
    {
        public string id;     // StableId (instance)
        public string defId;  // BuildingDefinition.id (fajta)
        public Vector3 pos;
        public float rotZ;
    }

    [Header("Building registry (BuildingDefinition SO-k)")]
    public BuildingDefinition[] buildingRegistry;
    Dictionary<string, BuildingDefinition> buildReg = new Dictionary<string, BuildingDefinition>();

    // Exteriorban lerakott épületek listája
    public List<PlacedBuilding> exteriorBuildings = new List<PlacedBuilding>();

    // ========= DAY MIRROR =========

    int currentDayMirror = 1;
    public int CurrentDayMirror => currentDayMirror;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // növény registry feltöltés
        plantReg.Clear();
        if (plantRegistry != null)
            foreach (var p in plantRegistry)
                if (p && !string.IsNullOrEmpty(p.id))
                    plantReg[p.id] = p;

        // building registry feltöltés
        buildReg.Clear();
        if (buildingRegistry != null)
            foreach (var b in buildingRegistry)
                if (b && !string.IsNullOrEmpty(b.id))
                    buildReg[b.id] = b;

        // nap tükör + offscreen növekedés
        if (DayNightSystem.Instance != null)
        {
            currentDayMirror = DayNightSystem.Instance.CurrentDay;
            DayNightSystem.Instance.OnDayAdvanced += OnDayAdvanced;
        }
    }

    void OnDestroy()
    {
        if (DayNightSystem.Instance != null)
            DayNightSystem.Instance.OnDayAdvanced -= OnDayAdvanced;
    }

    void OnDayAdvanced(int newDay)
    {
        currentDayMirror = newDay;

        // OFFSCREEN növekedés minden ültetett növényre:
        foreach (var gh in beds.Values)
            foreach (var s in gh.Values)
            {
                if (!s.hasPlant) continue;
                if (s.stage == PlantStage.Fruiting || s.stage == PlantStage.Withered) continue;

                s.daysLeftInStage--;
                if (s.daysLeftInStage <= 0)
                {
                    var def = ResolveDef(s.plantDefId);
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
                    }
                }
                // FIGYELEM: locsolás flag-et itt NEM nullázzuk,
                // azt napváltáskor a BedPlot saját logikája kezeli, vagy
                // a wateredToday== (lastWateredDay==Today) ellenőrzés miatt amúgy is napi szintű.
            }
    }

    // ========= PLANT API =========

    public PlantDefinition ResolveDef(string plantDefId)
        => (!string.IsNullOrEmpty(plantDefId) && plantReg.TryGetValue(plantDefId, out var d)) ? d : null;

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

    /// Víz jelölés: ezen a napon locsolva volt.
    public void MarkWatered(string greenhouseId, string bedId, int day)
    {
        var s = GetOrCreateBed(greenhouseId, bedId);
        if (s == null) return;
        s.lastWateredDay = day;
    }

    /// Ellenőrzés: ma (day) locsolva volt-e?
    public bool IsWatered(string greenhouseId, string bedId, int day)
    {
        var s = GetOrCreateBed(greenhouseId, bedId);
        if (s == null) return false;
        return s.lastWateredDay == day;
    }

    /// Növény állapot kiírása mentésbe (overload 1: watered külön trackelve)
    public void WritePlant(string greenhouseId, string bedId,
                           PlantDefinition def, PlantStage stage, int daysLeft)
    {
        var s = GetOrCreateBed(greenhouseId, bedId);
        if (s == null) return;

        s.hasPlant = def != null;
        s.plantDefId = def ? def.id : null;
        s.stage = stage;
        s.daysLeftInStage = daysLeft;
        // locsolás marad a lastWateredDay mezőben
    }

    /// Overload 2 kompatibilitás kedvéért (ha a régi kód még hívná):
    public void WritePlant(string greenhouseId, string bedId,
                           PlantDefinition def, PlantStage stage, int daysLeft, bool wateredToday)
    {
        WritePlant(greenhouseId, bedId, def, stage, daysLeft);
        // opcionálisan beállítjuk a mai napra a locsolást, ha kérted
        if (wateredToday)
            MarkWatered(greenhouseId, bedId,
                DayNightSystem.Instance ? DayNightSystem.Instance.CurrentDay : currentDayMirror);
    }

    // ========= BUILDINGS API =========

    public BuildingDefinition BuildingDef(string id)
        => (!string.IsNullOrEmpty(id) && buildReg.TryGetValue(id, out var d)) ? d : null;

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
            e.defId = defId; e.pos = pos; e.rotZ = rotZ;
        }
    }

    public void RemovePlaced(string stableId)
    {
        if (string.IsNullOrEmpty(stableId)) return;
        exteriorBuildings.RemoveAll(p => p.id == stableId);
    }
}
