using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Seeds")]
    public int aerolgaeSeeds = 0;
    public int carrotSeeds = 0;

    [Header("Produce (harvested)")]
    public int oxygenUnits = 0; // Aerolgae-ból
    public int foodUnits = 0; // Carrot-ból

    [Header("Seed refs (SO)")]
    public SeedItem aerolgaeSeedRef;
    public SeedItem carrotSeedRef;

    // === UI/observereknek ===
    public event Action OnChanged;
    public void RaiseChanged() => OnChanged?.Invoke();

    public bool HasSeed(SeedItem seed)
    {
        if (seed == aerolgaeSeedRef) return aerolgaeSeeds > 0;
        if (seed == carrotSeedRef) return carrotSeeds > 0;
        return false;
    }

    public bool ConsumeSeed(SeedItem seed, int count = 1)
    {
        bool ok = false;
        if (seed == aerolgaeSeedRef && aerolgaeSeeds >= count) { aerolgaeSeeds -= count; ok = true; }
        else if (seed == carrotSeedRef && carrotSeeds >= count) { carrotSeeds -= count; ok = true; }

        if (ok) RaiseChanged();
        return ok;
    }

    public void AddProduce(ProduceType type, int amount)
    {
        if (amount <= 0) return;
        if (type == ProduceType.Oxygen) oxygenUnits += amount;
        else if (type == ProduceType.Food) foodUnits += amount;
        RaiseChanged();
    }

    // Ha UI nélkül automatikusan választasz
    public SeedItem GetAutoSeedChoice()
    {
        if (aerolgaeSeeds > 0) return aerolgaeSeedRef;
        if (carrotSeeds > 0) return carrotSeedRef;
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

        return list;
    }

    public SeedItem FindSeedByDef(PlantDefinition def)
    {
        if (!def) return null;
        if (aerolgaeSeedRef && aerolgaeSeedRef.plant == def) return aerolgaeSeedRef;
        if (carrotSeedRef && carrotSeedRef.plant == def) return carrotSeedRef;
        return null;
    }

    // === Watering Can support ===
    WateringCan _carriedCan;
    public bool HasWateringCan => _carriedCan != null;
    public WateringCan CurrentCan => _carriedCan;

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
}
