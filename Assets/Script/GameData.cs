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

    [Header("Plant revive settings")]
    public int reviveMinDaysLeft = 1;

    [Serializable]
    public class BedSave
    {
        public bool hasPlant;
        public string plantDefId;
        public PlantStage stage;
        public int daysLeftInStage;

        public int lastWateredDay = int.MinValue;
        public int missedWaterDays = 0;

        // Withered előtti állapot
        public PlantStage? prevStageBeforeWither = null;
        public int? prevDaysLeftBeforeWither = null;   // <<< ÚJ
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

    [System.Serializable]
    public class IronRespawnSave
    {
        public bool available;       // elérhető-e a pickup (látszik-e)
        public int nextAvailableDay; // mikor spawnol újra; int.MinValue = most elérhető
    }
    // StableId -> respawn állapot
    private readonly Dictionary<string, IronRespawnSave> ironRespawns = new();

    // --- PERSIST API az IronPickup-hoz ---

    /// Mentett állapot lekérése. Visszatér true, ha találtunk mentést.
    public bool TryGetIronRespawn(string stableId, out bool available, out int nextAvailableDay)
    {
        available = true;
        nextAvailableDay = int.MinValue;

        if (string.IsNullOrEmpty(stableId)) return false;
        if (ironRespawns.TryGetValue(stableId, out var s) && s != null)
        {
            available = s.available;
            nextAvailableDay = s.nextAvailableDay;
            return true;
        }
        return false;
    }

    /// Mentés/overwrite az adott StableId-hez.
    public void WriteIronRespawn(string stableId, bool available, int nextAvailableDay)
    {
        if (string.IsNullOrEmpty(stableId)) return;

        if (!ironRespawns.TryGetValue(stableId, out var s) || s == null)
        {
            s = new IronRespawnSave();
            ironRespawns[stableId] = s;
        }
        s.available = available;
        s.nextAvailableDay = nextAvailableDay;
    }

    /// Teljes törlés (pl. ha destroy-olod a pickupot).
    public void ClearIronRespawn(string stableId)
    {
        if (string.IsNullOrEmpty(stableId)) return;
        ironRespawns.Remove(stableId);
    }


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
    int ClampReviveDays(int? savedDays)
    {
        int d = savedDays.HasValue ? savedDays.Value : 0;
        return Mathf.Max(reviveMinDaysLeft, d);
    }


    private PlantDefinition PlantDefById(string id)
        => (!string.IsNullOrEmpty(id) && plantReg.TryGetValue(id, out var d)) ? d : null;

    public PlantDefinition ResolveDef(string plantDefId) => PlantDefById(plantDefId);

    public BuildingDefinition BuildingDef(string id)
        => (!string.IsNullOrEmpty(id) && buildReg.TryGetValue(id, out var d)) ? d : null;


    private void EnsureMinDaysForStage(BedSave s)
    {
        if (s == null) return;
        if (s.daysLeftInStage > 0 && s.daysLeftInStage >= reviveMinDaysLeft) return;

        var def = PlantDefById(s.plantDefId);
        // Ha nincs definíció vagy 0-ra futottunk, akkor legalább reviveMinDaysLeft legyen
        int min = Mathf.Max(1, reviveMinDaysLeft);

        if (def == null)
        {
            s.daysLeftInStage = min;
            return;
        }

        switch (s.stage)
        {
            case PlantStage.Seed:
                s.daysLeftInStage = Mathf.Max(min, def.daysSeedToSapling > 0 ? 1 : min);
                break;
            case PlantStage.Sapling:
                s.daysLeftInStage = Mathf.Max(min, 1); // nem kell pontos érték, a min a lényeg
                break;
            case PlantStage.Mature:
                s.daysLeftInStage = Mathf.Max(min, 1);
                break;
            case PlantStage.Fruiting:
                // Fruitingben nincs számláló – itt nem kell semmi
                break;
        }
    }

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
        s.missedWaterDays = 0;

        if (s.hasPlant && s.stage == PlantStage.Withered && s.prevStageBeforeWither.HasValue)
        {
            s.stage = s.prevStageBeforeWither.Value;
            s.daysLeftInStage = ClampReviveDays(s.prevDaysLeftBeforeWither);
            s.prevStageBeforeWither = null;
            s.prevDaysLeftBeforeWither = null;
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


    // Megpróbáljuk kitalálni, melyik stádiumból hervadt el.
    // A Withered-be kerüléskor NEM nullázzuk a daysLeftInStage-et, így az
    // általában annak a stádiumidőnek a visszaszámlálója, amiben épp volt.
    private PlantStage InferPrevStage(BedSave s)
    {
        var def = PlantDefById(s.plantDefId);
        if (def == null) return PlantStage.Sapling; // safe fallback

        // Ha már termő volt, onnan nem hervasztunk tovább – de biztos ami biztos:
        // (általában a Fruitingnál daysLeftInStage = 0)
        if (s.daysLeftInStage <= 0)
        {
            // ha 0, valószínűleg Mature-ból lépett volna Fruitingba, vagy már Fruiting volt
            return PlantStage.Mature;
        }

        // Ha még sok nap volt hátra, valószínű Seed vagy Sapling állapotból jött.
        // A határokat a definíciók idejéből számoljuk.
        int toSapling = Mathf.Max(1, def.daysSeedToSapling);
        int toMature = Mathf.Max(1, def.daysSaplingToMature);
        int toFruiting = Mathf.Max(1, def.daysMatureToFruiting);

        // Heurisztika: ha még nagy stádiumidő volt hátra, inkább az adott stádiumban maradjon.
        // (Nem kritikus, csak hogy legyen értelmes visszaállítás.)
        if (s.daysLeftInStage > toMature) return PlantStage.Seed;
        if (s.daysLeftInStage > toFruiting) return PlantStage.Sapling;
        return PlantStage.Mature;
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

                // ---- NEM volt locsolva az előző napon ----
                if (!wateredPrevDay)
                {
                    // Seed kivétel
                    if (s.stage == PlantStage.Seed)
                    {
                        s.missedWaterDays = 0;
                        continue;
                    }

                    // Első száraz nap -> Withered
                    if (s.stage != PlantStage.Withered)
                    {
                        if (!s.prevStageBeforeWither.HasValue)
                        {
                            s.prevStageBeforeWither = s.stage;
                            // mentjük a hátralévő napokat, de legalább 1-et
                            s.prevDaysLeftBeforeWither = ClampReviveDays(s.daysLeftInStage);
                        }

                        s.stage = PlantStage.Withered;
                        s.missedWaterDays = 1;
                    }
                    else
                    {
                        // Második egymás utáni száraz nap -> megsemmisül
                        s.missedWaterDays++;
                        if (s.missedWaterDays >= 2)
                        {
                            s.hasPlant = false;
                            s.plantDefId = null;
                            s.stage = PlantStage.Seed;
                            s.daysLeftInStage = 0;
                            s.prevStageBeforeWither = null;
                            s.prevDaysLeftBeforeWither = null; // <<< töröljük
                            s.lastWateredDay = int.MinValue;
                            s.missedWaterDays = 0;
                        }
                    }

                    // száraz napon nincs növekedés
                    continue;
                }

                // ---- locsolva volt az előző napon ----
                s.missedWaterDays = 0;

                // Ha withered és van mentett előző állapot: állítsuk vissza MOST
                if (s.stage == PlantStage.Withered && s.prevStageBeforeWither.HasValue)
                {
                    s.stage = s.prevStageBeforeWither.Value;
                    s.daysLeftInStage = ClampReviveDays(s.prevDaysLeftBeforeWither); // <<< napok vissza
                    s.prevStageBeforeWither = null;
                    s.prevDaysLeftBeforeWither = null;
                }

                // növekedés (csak locsolt napon)
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
                            s.daysLeftInStage = 0; // termő állapotban marad
                            break;

                        case PlantStage.Fruiting:
                            // marad termő
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
