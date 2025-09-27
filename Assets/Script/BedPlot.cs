using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BedPlot : MonoBehaviour, IInteractable
{
    [Header("Visuals")]
    [SerializeField] SpriteRenderer soilRenderer;
    [SerializeField] Transform plantAnchor;

    [Header("Watering")]
    [SerializeField] float waterCostPerWatering = 10f;

    // --- LOGIKA: 1 növény ---
    PlantActor currentPlant; // a KÖZÉPSŐ példány – ebből mentünk/olvasunk

    // --- VIZUÁL: 3 példány ugyanahhoz a növényhez ---
    readonly List<PlantActor> visualPlants = new List<PlantActor>(3);

    // TravelContext + lokális ágyás-azonosító -> kulcs
    string greenhouseIdCached;
    string bedKeyCached;       // pl. "GH_12ab3:A"
    string localBedIdCached;   // pl. "A"

    int lastWateredDayLocal = int.MinValue;

    // ========= UI helper =========
    public bool HasPlant => currentPlant != null;
    public bool IsFruiting => currentPlant != null && currentPlant.stage == PlantStage.Fruiting;
    public PlantDefinition CurrentDef => currentPlant ? currentPlant.def : null;

    public bool IsWateredToday()
    {
        int day = Today();
        if (day < 0) return false;
        if (lastWateredDayLocal == day) return true;
        if (GameData.I == null || !KeysValid()) return false;
        return GameData.I.IsWatered(greenhouseIdCached, bedKeyCached, day);
    }
    // =============================

    void Awake()
    {
        greenhouseIdCached = TravelContext.currentGreenhouseId;
        var bli = GetComponent<BedLocalId>();
        localBedIdCached = bli ? bli.localId : name; // fallback: GameObject neve
        RebuildBedKey();

        if (!plantAnchor) plantAnchor = transform;
    }

    void RebuildBedKey()
    {
        if (!string.IsNullOrEmpty(greenhouseIdCached) && !string.IsNullOrEmpty(localBedIdCached))
            bedKeyCached = $"{greenhouseIdCached}:{localBedIdCached}";
        else
            bedKeyCached = null;
    }

    int Today()
    {
        if (DayNightSystem.Instance != null) return DayNightSystem.Instance.CurrentDay;
        return GameData.I ? GameData.I.CurrentDayMirror : -1;
    }

    bool KeysValid()
    {
        bool ok = !string.IsNullOrEmpty(greenhouseIdCached) && !string.IsNullOrEmpty(bedKeyCached);
        if (!ok)
            Debug.LogWarning($"[BedPlot] MISSING KEY(s) gh='{greenhouseIdCached}' bedKey='{bedKeyCached}' on {name}");
        return ok;
    }

    bool WateredToday
    {
        get
        {
            int day = Today();
            if (day < 0) return false;
            if (lastWateredDayLocal == day) return true;
            if (GameData.I == null || !KeysValid()) return false;
            return GameData.I.IsWatered(greenhouseIdCached, bedKeyCached, day);
        }
    }

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnEnable()
    {
        if (string.IsNullOrEmpty(greenhouseIdCached))
            greenhouseIdCached = TravelContext.currentGreenhouseId;
        RebuildBedKey();

        RestoreFromSave();

        if (DayNightSystem.Instance != null)
            DayNightSystem.Instance.OnDayAdvanced += OnNewDay;
    }

    void OnDisable()
    {
        if (DayNightSystem.Instance != null)
            DayNightSystem.Instance.OnDayAdvanced -= OnNewDay;
    }

    void OnNewDay(int day) => RestoreFromSave();


    void ProgressOneDay(int newDay)
    {
        // ha valamiért nincs betöltve a currentPlant, próbáld helyreállítani
        if (currentPlant == null)
        {
            RestoreFromSave();
            if (currentPlant == null) return;
        }

        // Fruiting/Withered nem nő tovább (Withered-et külön rendszer hozhatja vissza)
        if (currentPlant.stage == PlantStage.Fruiting || currentPlant.stage == PlantStage.Withered)
        {
            // csak a vizuál/mentés szinkron kedvéért
            WriteSave();
            return;
        }

        // tegnapi locsolás számít a ma reggeli növekedéshez
        int prevDay = newDay - 1;
        bool wasWateredYesterday =
            GameData.I != null && KeysValid() &&
            GameData.I.IsWatered(greenhouseIdCached, bedKeyCached, prevDay);

        if (!wasWateredYesterday)
        {
            // nem volt locsolva → nincs előrelépés (ide teheted később a száradás/withering logikát)
            WriteSave();
            return;
        }

        int daysLeft = currentPlant.GetDaysLeftExternal() - 1;
        var stage = currentPlant.stage;

        if (daysLeft <= 0)
        {
            var def = currentPlant.def;
            switch (stage)
            {
                case PlantStage.Seed:
                    stage = PlantStage.Sapling;
                    daysLeft = Mathf.Max(0, def ? def.daysSaplingToMature : 0);
                    break;
                case PlantStage.Sapling:
                    stage = PlantStage.Mature;
                    daysLeft = Mathf.Max(0, def ? def.daysMatureToFruiting : 0);
                    break;
                case PlantStage.Mature:
                    stage = PlantStage.Fruiting;
                    daysLeft = 0;
                    break;
            }
        }

        // állapot + vizuál frissítés + mentés
        ForcePlantState(currentPlant, stage, daysLeft);
        MirrorFromCurrent();
        WriteSave();
    }

    void AdvanceGrowthInSave(int newDay)
    {
        var s = GameData.I.GetOrCreateBed(greenhouseIdCached, bedKeyCached);
        if (s == null || !s.hasPlant || string.IsNullOrEmpty(s.plantDefId)) return;

        // Fruiting/Withered: itt már nincs növekedés (witheringet később lehet ide tenni)
        if (s.stage == PlantStage.Fruiting || s.stage == PlantStage.Withered) return;

        // tegnapi locsolás számít a ma reggeli növekedéshez
        int prevDay = newDay - 1;
        bool wateredYesterday = GameData.I.IsWatered(greenhouseIdCached, bedKeyCached, prevDay);
        if (!wateredYesterday)
        {
            // nem locsoltad → most nem lépünk (ide tehetsz később "száradás" logikát)
            return;
        }

        // csökkentjük a hátralévő napokat és szükség esetén stádiumot váltunk
        int daysLeft = Mathf.Max(0, s.daysLeftInStage - 1);
        var stage = s.stage;

        var def = GameData.I.ResolveDef(s.plantDefId);
        if (!def) return;

        if (daysLeft <= 0)
        {
            switch (stage)
            {
                case PlantStage.Seed:
                    stage = PlantStage.Sapling;
                    daysLeft = Mathf.Max(1, def.daysSaplingToMature);
                    break;
                case PlantStage.Sapling:
                    stage = PlantStage.Mature;
                    daysLeft = Mathf.Max(1, def.daysMatureToFruiting);
                    break;
                case PlantStage.Mature:
                    stage = PlantStage.Fruiting;
                    daysLeft = 0;
                    break;
            }
        }

        // visszaírjuk a mentésbe – innen olvassa majd a RestoreFromSave()
        GameData.I.WritePlant(greenhouseIdCached, bedKeyCached, def, stage, daysLeft);
        Debug.Log($"[BedPlot] Day {newDay}: wateredYesterday={wateredYesterday}, stage={s.stage}, left={s.daysLeftInStage}");
        if (GameData.I == null || !KeysValid()) return;
    }
    // ----------------- VIZUÁL HELPER-EK -----------------

    void ClearVisuals()
    {
        visualPlants.Clear();
        if (!plantAnchor) return;

        // csak a növényeket töröljük az anchor alól
        var toDestroy = new List<GameObject>();
        foreach (Transform t in plantAnchor)
            toDestroy.Add(t.gameObject);
        foreach (var go in toDestroy)
            Destroy(go);
    }

    void SpawnVisualPlants(PlantDefinition def, PlantStage stage, int daysLeft)
    {
        ClearVisuals();

        // 3 fix offset: bal–közép–jobb
        Vector3[] offsets = new[]
        {
            new Vector3(-0.8f, 0f, 0f),
            Vector3.zero,
            new Vector3( 0.8f, 0f, 0f)
        };

        for (int i = 0; i < 3; i++)
        {
            var go = new GameObject($"Plant_{i}");
            go.transform.SetParent(plantAnchor, false);
            go.transform.localPosition = offsets[i];

            var sr = go.AddComponent<SpriteRenderer>();
            if (soilRenderer) sr.sortingOrder = soilRenderer.sortingOrder + 1;

            var actor = go.AddComponent<PlantActor>();
            actor.Init(def);
            ForcePlantState(actor, stage, daysLeft);

            visualPlants.Add(actor);
        }

        // a középső legyen a "fő" példány (amivel mentünk)
        currentPlant = visualPlants.Count >= 2 ? visualPlants[1] : visualPlants[0];
    }

    void MirrorFromCurrent()
    {
        if (currentPlant == null) return;
        foreach (var actor in visualPlants)
        {
            if (actor == null || actor == currentPlant) continue;
            // tükrözzük a stádiumot + hátralévő napokat
            actor.def = currentPlant.def;
            ForcePlantState(actor, currentPlant.stage, currentPlant.GetDaysLeftExternal());
        }
    }

    // ----------------------------------------------------

    void RestoreFromSave()
    {
        if (GameData.I == null || !KeysValid()) return;

        var s = GameData.I.GetOrCreateBed(greenhouseIdCached, bedKeyCached);
        if (s == null) return;

        if (s.hasPlant && !string.IsNullOrEmpty(s.plantDefId))
        {
            var def = GameData.I.ResolveDef(s.plantDefId);
            if (def == null) return;

            // --- CATCH-UP: pótoljuk a kimaradt napokat a mentésben ---
            CatchUpGrowthInSave(def);

            // friss állapot (mert fent írhattunk a mentésbe)
            s = GameData.I.GetOrCreateBed(greenhouseIdCached, bedKeyCached);

            // három vizuális példányt készítünk
            SpawnVisualPlants(def, s.stage, s.daysLeftInStage);

            lastWateredDayLocal = s.lastWateredDay; // cache szinkron
        }
        else
        {
            ClearVisuals();
            currentPlant = null;
        }
    }


    void ForcePlantState(PlantActor plant, PlantStage stage, int daysLeft)
    {
        plant.stage = stage;

        var sr = plant.GetComponent<SpriteRenderer>();
        if (sr && plant.def)
        {
            sr.sprite = stage switch
            {
                PlantStage.Seed => plant.def.seedSprite,
                PlantStage.Sapling => plant.def.saplingSprite,
                PlantStage.Mature => plant.def.matureSprite,
                PlantStage.Fruiting => plant.def.fruitingSprite,
                PlantStage.Withered => plant.def.witheredSprite,
                _ => null
            };
        }
        plant.SetDaysLeftForExternalRestore(daysLeft);
    }

    public string GetPrompt()
    {
        if (currentPlant == null) return "Plant seed (E)";
        return "Open plant status (E)";
    }

    public void Interact(PlayerStats player)
    {
        if (!player) return;
        var inv = player.GetComponent<PlayerInventory>();
        if (!inv) { Debug.LogWarning("[BedPlot] PlayerInventory missing."); return; }

        // Ültetés – felugró választó
        if (!currentPlant)
        {
            var options = inv.GetSeedOptions();
            if (options == null || options.Count == 0)
            {
                Debug.Log("[BedPlot] No seeds.");
                return;
            }

            if (SeedSelectionUI.Instance == null)
            {
                Debug.LogError("[BedPlot] SeedSelectionUI.Instance is null – tedd a Canvasra és állítsd be a referenciákat.");
                return;
            }

            SeedSelectionUI.Instance.Show(options, (pickedDef) =>
            {
                if (pickedDef == null) return;

                var seedItem = inv.FindSeedByDef(pickedDef);
                if (seedItem == null) { Debug.LogWarning("[BedPlot] SeedItem not found for picked PlantDefinition."); return; }
                if (!inv.ConsumeSeed(seedItem, 1)) { Debug.Log("[BedPlot] Out of that seed."); return; }

                // LOGIKA + VIZUÁL: három példány azonnal
                SpawnVisualPlants(pickedDef, PlantStage.Seed, Mathf.Max(1, pickedDef.daysSeedToSapling));

                // Mentés + locsolás reset
                WriteSave();
                if (GameData.I != null && KeysValid())
                {
                    lastWateredDayLocal = int.MinValue;
                    GameData.I.MarkWatered(greenhouseIdCached, bedKeyCached, int.MinValue);
                }
            });

            return;
        }

        // VAN növény: status panel
        if (PlantStatusUI.Instance != null) PlantStatusUI.Instance.ShowFor(this);
        else Debug.LogWarning("[BedPlot] PlantStatusUI.Instance is null – tedd a Canvasra és állítsd be a referenciáit.");
    }

    // Locsolás
    public void Water(PlayerStats player)
    {
        if (!player) return;
        var inv = player.GetComponent<PlayerInventory>();
        if (!inv || !inv.HasWateringCan) { Debug.Log("[BedPlot] You need a watering can."); return; }

        if (!currentPlant) { Debug.Log("[BedPlot] Nothing to water."); return; }
        if (WateredToday) { Debug.Log("[BedPlot] Already watered today."); return; }

        float need = Mathf.Max(player.minWaterToWater, waterCostPerWatering);
        if (!player.TryConsumeWater(need)) { Debug.Log("[BedPlot] Not enough water."); return; }

        int day = Today();

        if (GameData.I != null && KeysValid())
        {
            GameData.I.MarkWatered(greenhouseIdCached, bedKeyCached, day);

            // If withered -> próbáljuk azonnal visszahozni
            if (GameData.I.TryReviveIfWithered(greenhouseIdCached, bedKeyCached, out var revivedStage))
            {
                var s = GameData.I.GetOrCreateBed(greenhouseIdCached, bedKeyCached);
                int daysLeftFromSave = s != null ? s.daysLeftInStage : currentPlant.GetDaysLeftExternal();
                ForcePlantState(currentPlant, revivedStage, daysLeftFromSave);
                MirrorFromCurrent();
            }
        }

        lastWateredDayLocal = day;
        WriteSave();
    }

    // ===== UI gombok által hívott műveletek =====

    public void DestroyPlantFromUI()
    {
        if (!currentPlant) return;

        ClearVisuals();
        currentPlant = null;

        if (GameData.I != null && KeysValid())
        {
            GameData.I.WritePlant(greenhouseIdCached, bedKeyCached, null, PlantStage.Seed, 0);
            GameData.I.MarkWatered(greenhouseIdCached, bedKeyCached, int.MinValue);
        }
        lastWateredDayLocal = int.MinValue;
    }

    public void HarvestFromUI()
    {
        if (!currentPlant) return;
        if (currentPlant.stage != PlantStage.Fruiting) return;

        var inv = FindFirstObjectByType<PlayerInventory>();
        if (inv != null)
        {
            var amt = currentPlant.Harvest(out var type);
            inv.AddProduce(type, amt);
        }

        // frissítsük a vizuálokat, majd írjuk ki a mentést
        MirrorFromCurrent();
        WriteSave();
    }

    void WriteSave()
    {
        if (GameData.I == null || !KeysValid()) return;

        if (!currentPlant)
        {
            GameData.I.WritePlant(greenhouseIdCached, bedKeyCached, null, PlantStage.Seed, 0);
            return;
        }

        int daysLeft = currentPlant.GetDaysLeftExternal();
        GameData.I.WritePlant(greenhouseIdCached, bedKeyCached, currentPlant.def, currentPlant.stage, daysLeft);
    }
    void CatchUpGrowthInSave(PlantDefinition def)
    {
        if (GameData.I == null || !KeysValid() || def == null) return;

        var s = GameData.I.GetOrCreateBed(greenhouseIdCached, bedKeyCached);
        if (s == null || !s.hasPlant || string.IsNullOrEmpty(s.plantDefId)) return;
        if (s.stage == PlantStage.Fruiting || s.stage == PlantStage.Withered) return;

        int today = Today();
        if (today < 0) return;

        // Ha sosem volt értelmes locsolás-nap, nincs mit pótolni
        int startDay = Mathf.Max(0, s.lastWateredDay);
        if (today <= startDay) return;

        // Kiinduló állapot
        var stage = s.stage;
        int daysLeft = Mathf.Max(0, s.daysLeftInStage);

        // Végigmegyünk a (lastWateredDay + 1) .. today napokon.
        // Minden nap elején a tegnapi locsolás számít.
        for (int day = startDay + 1; day <= today; day++)
        {
            bool wateredYesterday = GameData.I.IsWatered(greenhouseIdCached, bedKeyCached, day - 1);
            if (!wateredYesterday) continue;

            daysLeft = Mathf.Max(0, daysLeft - 1);

            if (daysLeft <= 0)
            {
                switch (stage)
                {
                    case PlantStage.Seed:
                        stage = PlantStage.Sapling;
                        daysLeft = Mathf.Max(1, def.daysSaplingToMature);
                        break;

                    case PlantStage.Sapling:
                        stage = PlantStage.Mature;
                        daysLeft = Mathf.Max(1, def.daysMatureToFruiting);
                        break;

                    case PlantStage.Mature:
                        stage = PlantStage.Fruiting;
                        daysLeft = 0;
                        break;
                }
            }
        }

        // Írjuk vissza – innen dolgozik a Restore vizuálja
        GameData.I.WritePlant(greenhouseIdCached, bedKeyCached, def, stage, daysLeft);

        Debug.Log($"[BedPlot] CatchUp bed={bedKeyCached}  from lastWatered={startDay} to today={today}  -> {stage}/{daysLeft}");
    }
}
