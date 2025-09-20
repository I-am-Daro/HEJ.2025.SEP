using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Seeds")]
    public int aerolgaeSeeds = 0;
    public int carrotSeeds = 0;
    public int ironBarkSeeds = 0;                 // <<< ÚJ: Iron Bark mag darabszám

    [Header("Produce (harvested)")]
    public int oxygenUnits = 0; // Aerolgae-ból
    public int foodUnits = 0;   // Carrot-ból

    [Header("Resources")]
    public int ironUnits = 0;   // építkezéshez használjuk; Iron Bark-ból is jöhet

    [Header("Seed refs (SO)")]
    public SeedItem aerolgaeSeedRef;
    public SeedItem carrotSeedRef;
    public SeedItem ironBarkSeedRef;              // <<< ÚJ: Iron Bark Seed SO referencia

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

    // termény felvétele (Harvest)
    public void AddProduce(ProduceType type, int amount)
    {
        if (amount <= 0) return;

        switch (type)
        {
            case ProduceType.Oxygen:
                oxygenUnits += amount;
                break;

            case ProduceType.Food:
                foodUnits += amount;
                break;

            case ProduceType.Iron:                  // <<< ÚJ: Iron mint termény → resource-hoz megy
                ironUnits += amount;
                break;
        }

        RaiseChanged();
    }

    // ----- Iron resource API -----
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

    // Ha UI nélkül automatikusan választasz
    public SeedItem GetAutoSeedChoice()
    {
        if (aerolgaeSeeds > 0) return aerolgaeSeedRef;
        if (carrotSeeds > 0) return carrotSeedRef;
        if (ironBarkSeeds > 0) return ironBarkSeedRef;   // <<< ÚJ
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

        if (ironBarkSeedRef && ironBarkSeeds > 0)   // <<< ÚJ blokk
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
        if (ironBarkSeedRef && ironBarkSeedRef.plant == def) return ironBarkSeedRef; // <<< ÚJ
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
