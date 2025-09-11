using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Seeds")]
    public int aerolgaeSeeds = 0;
    public int carrotSeeds = 0;

    [Header("Produce")]
    public int oxygenUnits = 0;
    public int foodUnits = 0;

    [Header("Seed refs (SO)")]
    public SeedItem aerolgaeSeedRef;
    public SeedItem carrotSeedRef;

    public bool HasSeed(SeedItem seed)
    {
        if (seed == aerolgaeSeedRef) return aerolgaeSeeds > 0;
        if (seed == carrotSeedRef) return carrotSeeds > 0;
        return false;
    }

    public void ConsumeSeed(SeedItem seed, int count = 1)
    {
        if (seed == aerolgaeSeedRef) aerolgaeSeeds = Mathf.Max(0, aerolgaeSeeds - count);
        else if (seed == carrotSeedRef) carrotSeeds = Mathf.Max(0, carrotSeeds - count);
    }

    public void AddProduce(ProduceType type, int amount)
    {
        if (type == ProduceType.Oxygen) oxygenUnits += amount;
        else if (type == ProduceType.Food) foodUnits += amount;
    }

    /// Ha nincs UI selection még: adja a "következõ elérhetõ" magot (prioritás sorrend)
    public SeedItem GetAutoSeedChoice()
    {
        if (aerolgaeSeeds > 0) return aerolgaeSeedRef;
        if (carrotSeeds > 0) return carrotSeedRef;
        return null;
    }
}
