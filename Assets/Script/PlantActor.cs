using UnityEngine;

public enum PlantStage { Seed, Sapling, Mature, Fruiting, Withered }

[RequireComponent(typeof(SpriteRenderer))]
public class PlantActor : MonoBehaviour
{
    public PlantDefinition def;
    public PlantStage stage = PlantStage.Seed;

    int daysLeftInStage;
    SpriteRenderer sr;

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    public void Init(PlantDefinition definition)
    {
        def = definition;
        InitStage(PlantStage.Seed);
    }

    void InitStage(PlantStage s)
    {
        stage = s;
        if (def == null) return;

        if (s == PlantStage.Seed) daysLeftInStage = Mathf.Max(1, def.daysSeedToSapling);
        else if (s == PlantStage.Sapling) daysLeftInStage = Mathf.Max(1, def.daysSaplingToMature);
        else if (s == PlantStage.Mature) daysLeftInStage = Mathf.Max(1, def.daysMatureToFruiting);
        else daysLeftInStage = 0;

        UpdateSprite();
    }

    void UpdateSprite()
    {
        if (!sr || def == null) return;
        sr.sprite = stage switch
        {
            PlantStage.Seed => def.seedSprite,
            PlantStage.Sapling => def.saplingSprite,
            PlantStage.Mature => def.matureSprite,
            PlantStage.Fruiting => def.fruitingSprite,
            PlantStage.Withered => def.witheredSprite,
            _ => null
        };
    }

    public void SetDaysLeftForExternalRestore(int days)
    {
        daysLeftInStage = Mathf.Max(0, days);
        UpdateSprite();
    }
    public int GetDaysLeftExternal() => daysLeftInStage;

    public int Harvest(out ProduceType type)
    {
        type = def.produceType;
        int amt = def.produceAmount;

        stage = PlantStage.Mature;
        daysLeftInStage = Mathf.Max(1, def.regrowDaysAfterHarvest);
        UpdateSprite();
        return amt;
    }
}
