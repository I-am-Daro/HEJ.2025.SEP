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

    int lastWateredDayLocal = int.MinValue; // helyi cache: melyik napon lett utoljára locsolva


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

            // Ha a helyi cache szerint ma már locsoltunk, az azonnal érvényes
            if (lastWateredDayLocal == day) return true;

            // Egyébként kérdezzük meg a GameData-t (stabilitás kedvéért)
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
            lastWateredDayLocal = s.lastWateredDay;

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
        if (currentPlant.stage == PlantStage.Fruiting) return "Harvest (E)";

        return WateredToday
            ? $"{currentPlant.def.displayName} (Watered)"
            : $"{currentPlant.def.displayName} – Water (F)";
    }

    public void Interact(PlayerStats player)
    {
        if (!player) return;
        var inv = player.GetComponent<PlayerInventory>();
        if (!inv) { UnityEngine.Debug.LogWarning("[BedPlot] PlayerInventory missing."); return; }

        if (!currentPlant)
        {
            var seed = inv.GetAutoSeedChoice();
            if (seed == null) { UnityEngine.Debug.Log("[BedPlot] No seeds."); return; }

            var go = new GameObject("Plant");
            go.transform.SetParent(plantAnchor ? plantAnchor : transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            if (soilRenderer) sr.sortingOrder = soilRenderer.sortingOrder + 1;

            currentPlant = go.AddComponent<PlantActor>();
            currentPlant.Init(seed.plant);
            inv.ConsumeSeed(seed, 1);

            WriteSave(); // seed+nap mentése

            // biztosan töröljük a locsolás napját (új növény)
            lastWateredDayLocal = int.MinValue;
            if (GameData.I != null && KeysValid())
                GameData.I.MarkWatered(greenhouseIdCached, bedIdCached, int.MinValue); 

            return;
        }

        if (currentPlant.stage == PlantStage.Fruiting)
        {
            var amt = currentPlant.Harvest(out var type);
            player.GetComponent<PlayerInventory>()?.AddProduce(type, amt);
            WriteSave();
        }
    }

    public void Water(PlayerStats player)
    {
        if (!player) return;
        if (!currentPlant) { UnityEngine.Debug.Log("[BedPlot] Nothing to water."); return; }

        if (WateredToday) { UnityEngine.Debug.Log("[BedPlot] Already watered today."); return; }

        float need = Mathf.Max(player.minWaterToWater, waterCostPerWatering);
        if (!player.TryConsumeWater(need))
        {
            UnityEngine.Debug.Log("[BedPlot] Not enough water.");
            return;
        }

        int day = Today();

        // Globális jelölés
        if (GameData.I != null && KeysValid())
            GameData.I.MarkWatered(greenhouseIdCached, bedIdCached, day);

        // Helyi azonnali tiltás ugyanarra a napra
        lastWateredDayLocal = day;

        WriteSave();
    }

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
