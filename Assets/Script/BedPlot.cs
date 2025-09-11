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
    string bedId => GetComponent<StableId>()?.Id;
    string greenhouseId => TravelContext.currentGreenhouseId;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnEnable()
    {
        RestoreFromSave(); // belépéskor építsük vissza a vizuált
        if (DayNightSystem.Instance != null)
            DayNightSystem.Instance.OnDayAdvanced += OnNewDay;
    }

    void OnDisable()
    {
        if (DayNightSystem.Instance != null)
            DayNightSystem.Instance.OnDayAdvanced -= OnNewDay;
    }

    void OnNewDay(int day)
    {
        // Napváltás után csak vizuál szinkron (off-screen nőtt)
        RestoreFromSave();
    }

    // Ma meg lett-e locsolva? (GameData alapján számoljuk)
    bool WateredToday
    {
        get
        {
            if (GameData.I == null || string.IsNullOrEmpty(greenhouseId) || string.IsNullOrEmpty(bedId))
                return false;
            int day = DayNightSystem.Instance ? DayNightSystem.Instance.CurrentDay : int.MinValue;
            return GameData.I.IsWatered(greenhouseId, bedId, day);
        }
    }

    void RestoreFromSave()
    {
        if (string.IsNullOrEmpty(bedId) || string.IsNullOrEmpty(greenhouseId) || GameData.I == null) return;

        var s = GameData.I.GetOrCreateBed(greenhouseId, bedId);

        if (currentPlant) { Destroy(currentPlant.gameObject); currentPlant = null; }

        if (s != null && s.hasPlant && !string.IsNullOrEmpty(s.plantDefId))
        {
            var def = GameData.I.ResolveDef(s.plantDefId);
            if (def)
            {
                var go = new GameObject("Plant");
                go.transform.SetParent(plantAnchor ? plantAnchor : transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                if (soilRenderer) sr.sortingOrder = soilRenderer.sortingOrder + 1;

                var plant = go.AddComponent<PlantActor>();
                plant.Init(def);

                // állítsuk a mentett stádiumra + nap számlálóra
                ForcePlantState(plant, s.stage, s.daysLeftInStage);

                currentPlant = plant;
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
        int d = DayNightSystem.Instance ? DayNightSystem.Instance.CurrentDay : -999;
        UnityEngine.Debug.Log($"[BedPlot] gh={greenhouseId} bed={bedId} day={d} wateredToday={WateredToday}");
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

        // Ültetés
        if (currentPlant == null)
        {
            var seed = inv.GetAutoSeedChoice();
            if (seed == null) { UnityEngine.Debug.Log("[BedPlot] No seeds."); return; }

            var go = new GameObject("Plant");
            go.transform.SetParent(plantAnchor ? plantAnchor : transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            if (soilRenderer) sr.sortingOrder = soilRenderer.sortingOrder + 1;

            var plant = go.AddComponent<PlantActor>();
            plant.Init(seed.plant);                  // Seed + def.daysSeedToSapling beáll
            currentPlant = plant;
            inv.ConsumeSeed(seed, 1);

            // Mentsük el az induló Seed állapotot a helyes nap-számlálóval
            WriteSave();
            if (GameData.I != null)
            {
                GameData.I.MarkWatered(
                    greenhouseId,
                    bedId,
                    int.MinValue   // „még soha” – így WateredToday biztosan false lesz
                );
            }
            return;
        }

        // Aratás
        if (currentPlant.stage == PlantStage.Fruiting)
        {
            var amt = currentPlant.Harvest(out var type);
            inv.AddProduce(type, amt);
            WriteSave(); // Mature + regrowDays mentése
        }
    }

    // Locsolás – PlayerInteractor.OnWater hívja
    public void Water(PlayerStats player)
    {
        if (!player) return;
        if (currentPlant == null) { UnityEngine.Debug.Log("[BedPlot] Nothing to water."); return; }
        if (WateredToday) { UnityEngine.Debug.Log("[BedPlot] Already watered today."); return; }

        float need = Mathf.Max(player.minWaterToWater, waterCostPerWatering);
        if (!player.TryConsumeWater(need))
        {
            UnityEngine.Debug.Log("[BedPlot] Not enough water.");
            return;
        }

        int day = DayNightSystem.Instance ? DayNightSystem.Instance.CurrentDay : 0;
        GameData.I?.MarkWatered(greenhouseId, bedId, day);

        WriteSave(); // opcionális itt, de nem árt
    }

    void WriteSave()
    {
        if (GameData.I == null || string.IsNullOrEmpty(bedId) || string.IsNullOrEmpty(greenhouseId)) return;

        if (currentPlant == null)
        {
            GameData.I.WritePlant(greenhouseId, bedId, null, PlantStage.Seed, 0);
            return;
        }

        int daysLeft = currentPlant.GetDaysLeftExternal();

        GameData.I.WritePlant(
            greenhouseId,
            bedId,
            currentPlant.def,
            currentPlant.stage,
            daysLeft
        );
    }
}
