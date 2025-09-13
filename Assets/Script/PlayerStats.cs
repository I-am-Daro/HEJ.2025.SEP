using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Range(0, 100)] public float oxygen = 100f;
    [Range(0, 100)] public float energy = 100f;
    [Range(0, 100)] public float hunger = 0f;

    // Víz (locsoláshoz)
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

    [Header("Starvation (Hunger==100) hatások")]
    public bool slowWhenStarving = true;
    public float starvingSpeedMultiplier = 0.33f;     // mozgató scriptek olvashatják (MoveSpeed *= this)
    public bool energyDrainBoostWhenStarving = true;
    public float starvingEnergyMultiplier = 1.5f;     // energiavesztés szorzó, ha éhezik

    [Header("Halál / respawn késleltetés (sec)")]
    public float suffocationDelay = 4f;               // O2 0-nál
    public float exhaustionDelay = 4f;               // Energy 0-nál

    // belső állapot
    enum DeathCause { None, Oxygen, Energy }
    DeathCause pendingDeath = DeathCause.None;
    bool isDying = false;
    float deathEndsAt = 0f;

    // Mozgás scripteknek hasznos lehet:
    public float MoveSpeedMultiplier => (slowWhenStarving && hunger >= 100f) ? starvingSpeedMultiplier : 1f;
    public bool IsStarving => hunger >= 100f;

    void Update()
    {
        // --- O2 ---
        if (isInShipInterior)
        {
            oxygen = Mathf.Min(100f, oxygen + oxygenRechargeInShip * Time.deltaTime);
        }
        else
        {
            float o2Drain = isZeroG ? oxygenDrainZeroG : oxygenDrainExterior;
            oxygen = Mathf.Clamp(oxygen - o2Drain * Time.deltaTime, 0f, 100f);
        }

        // --- Hunger ---
        hunger = Mathf.Clamp(hunger + hungerIncreasePerSec * Time.deltaTime, 0f, 100f);

        // --- Energy (éhezéskor gyorsabban fogy) ---
        float energyMult = (energyDrainBoostWhenStarving && hunger >= 100f) ? starvingEnergyMultiplier : 1f;
        energy = Mathf.Clamp(energy - energyDrainPerSec * energyMult * Time.deltaTime, 0f, 100f);

        // --- Halál / respawn logika ---
        if (!isDying)
        {
            if (oxygen <= 0f)
            {
                StartDeathCountdown(DeathCause.Oxygen, Mathf.Max(0.01f, suffocationDelay));
            }
            else if (energy <= 0f)
            {
                StartDeathCountdown(DeathCause.Energy, Mathf.Max(0.01f, exhaustionDelay));
            }
        }
        else
        {
            // ha visszatöltődött a kritikus stat a határidő előtt, megszakítjuk
            if ((pendingDeath == DeathCause.Oxygen && oxygen > 0f) ||
                (pendingDeath == DeathCause.Energy && energy > 0f))
            {
                CancelDeath();
            }
            else if (Time.time >= deathEndsAt)
            {
                // visszatöltjük a mentett napot/pozíciót
                isDying = false;
                pendingDeath = DeathCause.None;
                SaveSystem.LoadCheckpointAndPlacePlayer();
                return;
            }
        }
    }

    void StartDeathCountdown(DeathCause cause, float delay)
    {
        pendingDeath = cause;
        isDying = true;
        deathEndsAt = Time.time + delay;
        // ide jöhet majd hang/anim (pl. fuldoklás)
    }

    void CancelDeath()
    {
        isDying = false;
        pendingDeath = DeathCause.None;
    }

    // --- Interakciókhoz használt segédek ---

    public void Eat(float amount)
        => hunger = Mathf.Clamp(hunger - amount, 0f, 100f);

    public void FullRest()
        => energy = 100f;

    public bool TryConsumeWater(float amount)
    {
        if (water < amount) return false;
        water = Mathf.Max(0f, water - amount);
        return true;
    }
}
