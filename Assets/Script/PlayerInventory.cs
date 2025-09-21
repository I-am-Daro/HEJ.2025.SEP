using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Seeds (counts)")]
    public int aerolgaeSeeds = 0;
    public int carrotSeeds = 0;
    public int ironBarkSeeds = 0;   // <<< ÚJ

    [Header("Produce (harvested)")]
    public int oxygenUnits = 0; // Aerolgae-ból
    public int foodUnits = 0;   // Carrot-ból

    [Header("Resources")]
    public int ironUnits = 0;   // építkezéshez
    public int waterUnits = 0;  // <<< ÚJ: nyers víz egységek (elemzésből, későbbi rendszerekhez)

    [Header("Seed refs (SO)")]
    public SeedItem aerolgaeSeedRef;
    public SeedItem carrotSeedRef;
    public SeedItem ironBarkSeedRef;   // <<< ÚJ

    [Header("Rock samples (for Analyzer)")]
    [SerializeField] private List<RockSampleDefinition> debugInspectorView = new(); // csak megjelenítésre
    private readonly Dictionary<RockSampleDefinition, int> rockSamples = new();     // valódi tároló

    // === UI/observereknek ===
    public event Action OnChanged;
    public void RaiseChanged() => OnChanged?.Invoke();

    // ----- Seeds -----
    public bool HasSeed(SeedItem seed)
    {
        if (seed == aerolgaeSeedRef) return aerolgaeSeeds > 0;
        if (seed == carrotSeedRef) return carrotSeeds > 0;
        if (seed == ironBarkSeedRef) return ironBarkSeeds > 0;
        return false;
    }

    public bool ConsumeSeed(SeedItem seed, int count = 1)
    {
        bool ok = false;
        if (seed == aerolgaeSeedRef && aerolgaeSeeds >= count) { aerolgaeSeeds -= count; ok = true; }
        else if (seed == carrotSeedRef && carrotSeeds >= count) { carrotSeeds -= count; ok = true; }
        else if (seed == ironBarkSeedRef && ironBarkSeeds >= count) { ironBarkSeeds -= count; ok = true; }

        if (ok) RaiseChanged();
        return ok;
    }

    /// Kényelmi: seed hozzáadása PlantDefinition alapján (Analyzer eredményhez hasznos)
    public void AddSeed(PlantDefinition def, int amount = 1)
    {
        if (def == null || amount <= 0) return;

        if (aerolgaeSeedRef && aerolgaeSeedRef.plant == def) aerolgaeSeeds += amount;
        else if (carrotSeedRef && carrotSeedRef.plant == def) carrotSeeds += amount;
        else if (ironBarkSeedRef && ironBarkSeedRef.plant == def) ironBarkSeeds += amount;
        else
        {
            Debug.LogWarning($"[Inventory] AddSeed: PlantDefinition '{def.name}' nincs seedRef-hez kötve.");
            return;
        }

        RaiseChanged();
    }

    public void AddProduce(ProduceType type, int amount)
    {
        if (amount <= 0) return;
        if (type == ProduceType.Oxygen) oxygenUnits += amount;
        else if (type == ProduceType.Food) foodUnits += amount;
        else if (type == ProduceType.Iron) ironUnits += amount; // ha valaha növény is termel vasat
        RaiseChanged();
    }

    // ----- Resources generic -----
    public void AddIron(int amount)
    {
        if (amount <= 0) return;
        ironUnits += amount;
        RaiseChanged();
    }
    public bool SpendIron(int amount)
    {
        if (amount <= 0) return true;
        if (ironUnits < amount) return false;
        ironUnits -= amount;
        RaiseChanged();
        return true;
    }
    public bool HasIron(int amount) => ironUnits >= Mathf.Max(0, amount);

    public void AddWater(int amount)
    {
        if (amount <= 0) return;
        waterUnits += amount;
        RaiseChanged();
    }

    /// Általános erőforrás-hozzáadás Analyzerhez
    public void AddResource(ResourceType t, int amount)
    {
        if (amount <= 0) return;
        switch (t)
        {
            case ResourceType.Iron: AddIron(amount); break;
            case ResourceType.Water: AddWater(amount); break;
            default: Debug.LogWarning($"[Inventory] Unknown ResourceType {t}"); break;
        }
    }

    // ----- Rock Samples (Analyzer) -----
    public int GetRockSampleCount(RockSampleDefinition def)
        => (def != null && rockSamples.TryGetValue(def, out var n)) ? n : 0;

    public void AddRockSample(RockSampleDefinition def, int amount = 1)
    {
        if (def == null || amount <= 0) return;
        rockSamples.TryGetValue(def, out var n);
        rockSamples[def] = n + amount;
        SyncDebugView();
        RaiseChanged();
    }

    public bool ConsumeRockSample(RockSampleDefinition def, int amount = 1)
    {
        if (def == null || amount <= 0) return false;
        if (!rockSamples.TryGetValue(def, out var n) || n < amount) return false;

        n -= amount;
        if (n <= 0) rockSamples.Remove(def);
        else rockSamples[def] = n;

        SyncDebugView();
        RaiseChanged();
        return true;
    }

    public IEnumerable<KeyValuePair<RockSampleDefinition, int>> EnumerateRockSamples() => rockSamples;

    void SyncDebugView()
    {
        // csak szerkesztői betekintéshez – nem kell használni
        debugInspectorView.Clear();
        foreach (var kv in rockSamples)
        {
            for (int i = 0; i < kv.Value; i++) debugInspectorView.Add(kv.Key);
        }
    }

    // Ha UI nélkül automatikusan választasz
    public SeedItem GetAutoSeedChoice()
    {
        if (aerolgaeSeeds > 0) return aerolgaeSeedRef;
        if (carrotSeeds > 0) return carrotSeedRef;
        if (ironBarkSeeds > 0) return ironBarkSeedRef;
        return null;
    }

    // Seed választó UI-hoz
    public List<SeedSelectionUI.SeedOption> GetSeedOptions()
    {
        var list = new List<SeedSelectionUI.SeedOption>();

        if (aerolgaeSeedRef && aerolgaeSeeds > 0)
        {
            list.Add(new SeedSelectionUI.SeedOption
            {
                def = aerolgaeSeedRef.plant,
                count = aerolgaeSeeds,
                icon = aerolgaeSeedRef.icon ? aerolgaeSeedRef.icon :
                       (aerolgaeSeedRef.plant ? aerolgaeSeedRef.plant.seedSprite : null)
            });
        }

        if (carrotSeedRef && carrotSeeds > 0)
        {
            list.Add(new SeedSelectionUI.SeedOption
            {
                def = carrotSeedRef.plant,
                count = carrotSeeds,
                icon = carrotSeedRef.icon ? carrotSeedRef.icon :
                       (carrotSeedRef.plant ? carrotSeedRef.plant.seedSprite : null)
            });
        }

        if (ironBarkSeedRef && ironBarkSeeds > 0)
        {
            list.Add(new SeedSelectionUI.SeedOption
            {
                def = ironBarkSeedRef.plant,
                count = ironBarkSeeds,
                icon = ironBarkSeedRef.icon ? ironBarkSeedRef.icon :
                       (ironBarkSeedRef.plant ? ironBarkSeedRef.plant.seedSprite : null)
            });
        }

        return list;
    }

    public SeedItem FindSeedByDef(PlantDefinition def)
    {
        if (!def) return null;
        if (aerolgaeSeedRef && aerolgaeSeedRef.plant == def) return aerolgaeSeedRef;
        if (carrotSeedRef && carrotSeedRef.plant == def) return carrotSeedRef;
        if (ironBarkSeedRef && ironBarkSeedRef.plant == def) return ironBarkSeedRef;
        return null;
    }

    // === Watering Can support ===
    public bool HasWateringCan => _carriedCan != null;
    private WateringCan _carriedCan;

    public bool TryTakeCan(WateringCan can)
    {
        if (_carriedCan != null || can == null) return false;
        _carriedCan = can;
        return true;
    }

    public bool TryReturnCan(WateringCan can)
    {
        if (_carriedCan != can) return false;
        _carriedCan = null;
        return true;
    }

    public WateringCan CurrentCan => _carriedCan;
}
