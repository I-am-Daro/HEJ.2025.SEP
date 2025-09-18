using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BedPlot : MonoBehaviour, IInteractable
{
    [Header("Visuals")]
    [SerializeField] SpriteRenderer soilRenderer;
    [SerializeField] Transform plantAnchor;

    [Header("Watering")]
    [SerializeField] float waterCostPerWatering = 10f;

    PlantActor currentPlant;
    string greenhouseIdCached;
    string bedIdCached;

    // helyi cache: melyik napon lett utoljára locsolva
    int lastWateredDayLocal = int.MinValue;

    // ========= UI-nak hasznos olvasók =========
    public bool HasPlant => currentPlant != null;
    public bool IsFruiting => currentPlant != null && currentPlant.stage == PlantStage.Fruiting;
    public PlantDefinition CurrentDef => currentPlant ? currentPlant.def : null;

    public bool IsWateredToday()
    {
        int day = Today();
        if (day < 0) return false;
        if (lastWateredDayLocal == day) return true;
        if (GameData.I == null || !KeysValid()) return false;
        return GameData.I.IsWatered(greenhouseIdCached, bedIdCached, day);
    }
    // ==========================================

    void Awake()
    {
        bedIdCached = GetComponent<StableId>()?.Id;
        greenhouseIdCached = TravelContext.currentGreenhouseId;
    }

    int Today()
    {
        if (DayNightSystem.Instance != null) return DayNightSystem.Instance.CurrentDay;
        return GameData.I ? GameData.I.CurrentDayMirror : -1;
    }

    bool KeysValid()
    {
        bool ok = !string.IsNullOrEmpty(bedIdCached) && !string.IsNullOrEmpty(greenhouseIdCached);
        if (!ok)
            UnityEngine.Debug.LogWarning($"[BedPlot] MISSING KEY(s) gh='{greenhouseIdCached}' bed='{bedIdCached}' on {name}");
        return ok;
    }

    bool WateredToday
    {
        get
        {
            int day = Today();
            if (day < 0) return false;
            if (lastWateredDayLocal == day) return true; // azonnali lokális tiltás
            if (GameData.I == null || !KeysValid()) return false;
            return GameData.I.IsWatered(greenhouseIdCached, bedIdCached, day);
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

    void RestoreFromSave()
    {
        if (GameData.I == null || !KeysValid()) return;

        var s = GameData.I.GetOrCreateBed(greenhouseIdCached, bedIdCached);
        if (s == null) return;

        if (s.hasPlant && !string.IsNullOrEmpty(s.plantDefId))
        {
            var def = GameData.I.ResolveDef(s.plantDefId);
            if (def == null) return;

            if (!currentPlant)
            {
                var go = new GameObject("Plant");
                go.transform.SetParent(plantAnchor ? plantAnchor : transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                if (soilRenderer) sr.sortingOrder = soilRenderer.sortingOrder + 1;

                currentPlant = go.AddComponent<PlantActor>();
                currentPlant.Init(def);
            }
            else
            {
                currentPlant.def = def;
            }

            ForcePlantState(currentPlant, s.stage, s.daysLeftInStage);
            lastWateredDayLocal = s.lastWateredDay; // cache szinkron
        }
        else
        {
            if (currentPlant)
            {
                Destroy(currentPlant.gameObject);
                currentPlant = null;
            }
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
        // korábban itt “Harvest (E)” volt – most a status panel nyílik
        return "Open plant status (E)";
    }

    public void Interact(PlayerStats player)
    {
        if (!player) return;
        var inv = player.GetComponent<PlayerInventory>();
        if (!inv) { UnityEngine.Debug.LogWarning("[BedPlot] PlayerInventory missing."); return; }

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

                // PlantDefinition -> SeedItem, levonás
                var seedItem = inv.FindSeedByDef(pickedDef);
                if (seedItem == null)
                {
                    Debug.LogWarning("[BedPlot] SeedItem not found for picked PlantDefinition.");
                    return;
                }
                if (!inv.ConsumeSeed(seedItem, 1))
                {
                    Debug.Log("[BedPlot] Out of that seed.");
                    return;
                }

                // Ültetés
                var go = new GameObject("Plant");
                go.transform.SetParent(plantAnchor ? plantAnchor : transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                if (soilRenderer) sr.sortingOrder = soilRenderer.sortingOrder + 1;

                currentPlant = go.AddComponent<PlantActor>();
                currentPlant.Init(pickedDef);

                // Mentés + locsolás reset
                WriteSave();
                if (GameData.I != null && KeysValid())
                {
                    lastWateredDayLocal = int.MinValue;
                    GameData.I.MarkWatered(greenhouseIdCached, bedIdCached, int.MinValue);
                }
            });

            return;
        }

        // VAN növény: status panel ugrik fel
        if (PlantStatusUI.Instance != null)
        {
            PlantStatusUI.Instance.ShowFor(this);
        }
        else
        {
            Debug.LogWarning("[BedPlot] PlantStatusUI.Instance is null – tedd a Canvasra és állítsd be a referenciáit.");
        }
    }

    // UI -> “Water (F)” továbbra is külön actionről megy (PlayerInteractor.OnWater)
    public void Water(PlayerStats player)
    {
        if (!player) return;

        var inv = player.GetComponent<PlayerInventory>();
        if (!inv || !inv.HasWateringCan)
        {
            UnityEngine.Debug.Log("[BedPlot] You need a watering can.");
            return;
        }

        if (!currentPlant) { UnityEngine.Debug.Log("[BedPlot] Nothing to water."); return; }
        if (WateredToday) { UnityEngine.Debug.Log("[BedPlot] Already watered today."); return; }

        float need = Mathf.Max(player.minWaterToWater, waterCostPerWatering);
        if (!player.TryConsumeWater(need))
        {
            UnityEngine.Debug.Log("[BedPlot] Not enough water.");
            return;
        }

        int day = Today();

        if (GameData.I != null && KeysValid())
        {
            // Jelöljük, hogy MA locsoltunk
            GameData.I.MarkWatered(greenhouseIdCached, bedIdCached, day);

            // Ha Withered volt, próbáljuk azonnal életre hozni (mentés alapján)
            if (GameData.I.TryReviveIfWithered(greenhouseIdCached, bedIdCached, out var revivedStage))
            {
                // Vizuál azonnali frissítése
                ForcePlantState(currentPlant, revivedStage, currentPlant.GetDaysLeftExternal());
            }
        }

        lastWateredDayLocal = day; // azonnali tiltás
        WriteSave();
    }

    // ===== UI gombok által hívott műveletek =====

    public void DestroyPlantFromUI()
    {
        if (!currentPlant) return;

        Destroy(currentPlant.gameObject);
        currentPlant = null;

        // mentés: üres ágyás
        if (GameData.I != null && KeysValid())
        {
            GameData.I.WritePlant(greenhouseIdCached, bedIdCached, null, PlantStage.Seed, 0);
            GameData.I.MarkWatered(greenhouseIdCached, bedIdCached, int.MinValue);
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

        WriteSave(); // Mature + regrow napok mentése
    }

    // ============================================

    void WriteSave()
    {
        if (GameData.I == null || !KeysValid()) return;

        if (!currentPlant)
        {
            GameData.I.WritePlant(greenhouseIdCached, bedIdCached, null, PlantStage.Seed, 0);
            return;
        }

        int daysLeft = currentPlant.GetDaysLeftExternal();
        GameData.I.WritePlant(greenhouseIdCached, bedIdCached, currentPlant.def, currentPlant.stage, daysLeft);
    }
}
