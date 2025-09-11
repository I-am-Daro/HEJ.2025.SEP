using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Range(0, 100)] public float oxygen = 100f;
    [Range(0, 100)] public float energy = 100f;
    [Range(0, 100)] public float hunger = 0f;

    // ÚJ: víz
    [Header("Water")]
    [Range(0, 100)] public float water = 100f;
    public float minWaterToWater = 5f; // ennyi alatt nem engedünk locsolni

    [Header("O2 / Energy / Hunger ráták")]
    public float oxygenDrainExterior = 2f;
    public float oxygenDrainZeroG = 4f;
    public float oxygenRechargeInShip = 15f;
    public float energyDrainPerSec = 3f;
    public float hungerIncreasePerSec = 2f;

    [Header("Flags")]
    public bool isZeroG = false;
    public bool isInShipInterior = false;

    [Header("Suffocation")]
    public float suffocationDelay = 4f;
    bool isSuffocating = false;
    float suffocationEndsAt = 0f;

    void Update()
    {
        // O2
        if (isInShipInterior) oxygen = Mathf.Min(100f, oxygen + oxygenRechargeInShip * Time.deltaTime);
        else
        {
            float o2Drain = isZeroG ? oxygenDrainZeroG : oxygenDrainExterior;
            oxygen = Mathf.Clamp(oxygen - o2Drain * Time.deltaTime, 0f, 100f);
        }

        // Energy & Hunger
        energy = Mathf.Clamp(energy - energyDrainPerSec * Time.deltaTime, 0f, 100f);
        hunger = Mathf.Clamp(hunger + hungerIncreasePerSec * Time.deltaTime, 0f, 100f);

        // Fulladás
        if (oxygen <= 0f)
        {
            if (!isSuffocating) { isSuffocating = true; suffocationEndsAt = Time.time + Mathf.Max(0.01f, suffocationDelay); }
            else if (Time.time >= suffocationEndsAt) { isSuffocating = false; SaveSystem.LoadCheckpointAndPlacePlayer(); return; }
        }
        else if (isSuffocating) { isSuffocating = false; }

        // (energia 0 → respawn továbbra is maradhat nálad)
    }

    public void Eat(float amount) => hunger = Mathf.Clamp(hunger - amount, 0f, 100f);
    public void FullRest() => energy = 100f;

    // ÚJ: víz fogyasztás locsoláskor
    public bool TryConsumeWater(float amount)
    {
        if (water < amount) return false;
        water = Mathf.Max(0f, water - amount);
        return true;
    }
}
