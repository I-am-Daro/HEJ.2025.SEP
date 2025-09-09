using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Range(0, 100)] public float oxygen = 100f;
    [Range(0, 100)] public float energy = 100f;
    [Range(0, 100)] public float hunger = 0f;

    [Header("Rates per second")]
    public float oxygenDrainExterior = 2f;
    public float oxygenDrainZeroG = 4f;
    public float oxygenRechargeInShip = 15f;
    public float energyDrainPerSec = 3f;
    public float hungerIncreasePerSec = 2f;

    [Header("Flags")]
    public bool isZeroG = false;
    public bool isInShipInterior = false;

    [Header("Suffocation (death on no O2)")]
    [Tooltip("Mennyi idő teljen el zéró oxigénnél, mielőtt 'meghal'.")]
    public float suffocationDelay = 4f;
    bool isSuffocating = false;
    float suffocationEndsAt = 0f;

    [Header("Hunger slow")]
    [Tooltip("Ha a hunger eléri a 100-at, erre a szorzóra esik a sebesség.")]
    [Range(0.05f, 1f)] public float hungerSlowMultiplier = 0.33f;
    public float hungerSlowThreshold = 100f;

    PlayerMovementService moveSvc;
    bool hungerSlowActive = false;

    void Awake()
    {
        moveSvc = GetComponent<PlayerMovementService>();
    }

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

        if (!hungerSlowActive && hunger >= hungerSlowThreshold)
        {
            hungerSlowActive = true;
            if (moveSvc) moveSvc.SetSpeedMultiplier(hungerSlowMultiplier);
        }
        else if (hungerSlowActive && hunger < hungerSlowThreshold)
        {
            hungerSlowActive = false;
            if (moveSvc) moveSvc.SetSpeedMultiplier(1f);
        }
        // --- Energia & Éhség ---
        energy = Mathf.Clamp(energy - energyDrainPerSec * Time.deltaTime, 0f, 100f);
        hunger = Mathf.Clamp(hunger + hungerIncreasePerSec * Time.deltaTime, 0f, 100f);

        // --- Fulladás logika ---
        if (oxygen <= 0f)
        {
            if (!isSuffocating)
            {
                StartSuffocation();
            }
            else
            {
                if (Time.time >= suffocationEndsAt)
                {
                    // Meghalás → visszatöltés a checkpointból
                    UnityEngine.Debug.Log("[Player] Suffocated. Respawning from checkpoint...");
                    isSuffocating = false; // tisztítás, mielőtt átlépünk
                    SaveSystem.LoadCheckpointAndPlacePlayer();
                    return;
                }
            }
        }
        else
        {
            // Van oxigén → ha épp fulladtunk, töröljük
            if (isSuffocating) CancelSuffocation();
        }

        // --- (Opcionális) halál energiára is ---
        if (energy <= 0f)
        {
            UnityEngine.Debug.LogWarning("[Player] Energy depleted. Respawning from checkpoint...");
            SaveSystem.LoadCheckpointAndPlacePlayer();
        }
    }

    void StartSuffocation()
    {
        isSuffocating = true;
        suffocationEndsAt = Time.time + Mathf.Max(0.01f, suffocationDelay);
        // TODO: itt szólhat a „fuldoklás” SFX, UI villogás, vignette, stb.
        // pl.: if (audioSource && gaspClip) audioSource.PlayOneShot(gaspClip);
        UnityEngine.Debug.Log("[Player] No O2 – suffocation countdown started.");
    }

    void CancelSuffocation()
    {
        isSuffocating = false;
        // TODO: itt állítsd le a SFX-et / UI-t
        UnityEngine.Debug.Log("[Player] O2 restored – suffocation canceled.");
    }

    public void Eat(float amount)
    {
        hunger = Mathf.Clamp(hunger - amount, 0f, 100f);
        // ha az evéssel 100 alá megy, a fenti Update vissza fogja állítani a szorzót 1.0-re
    }
    public void FullRest() => energy = 100f;
}
