using System;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Collider2D))]
public class RockPickup : MonoBehaviour, IInteractable
{
    [Header("Rock Sample")]
    public RockSampleDefinition sampleDef;
    [Min(1)] public int amount = 1;

    [Header("Pickup behaviour")]
    [Tooltip("Ha igaz, ütközéskor azonnal felvétel történik.")]
    public bool pickupOnTrigger = false;

    [Header("Respawn (persistent)")]
    [Tooltip("Ha igaz, a minta eltűnik felvétel után és X nap múlva újra megjelenik.")]
    public bool enableRespawn = true;
    public int respawnDaysMin = 1;
    public int respawnDaysMax = 3;
    [Tooltip("Első belépéskor is várjon-e (ne legyen azonnal elérhető).")]
    public bool startOnCooldown = false;

    [Header("Destroy behaviour")]
    [Tooltip("Általában FALSE respawnhoz. Ha igaz, tényleg törli az objektumot a felvétel után.")]
    public bool destroyOnPickup = false;

    // ---- cache / state ----
    SpriteRenderer sr;
    Collider2D col;
    string stableId;

    bool available = true;
    int nextAvailableDay = int.MinValue;

    void Reset()
    {
        var c = GetComponent<Collider2D>();
        c.isTrigger = true;
    }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        // Stabil azonosító
        var sid = GetComponent<StableId>();
        if (!sid) sid = gameObject.AddComponent<StableId>();
        if (string.IsNullOrEmpty(sid.Id)) sid.AssignNewRuntimeId();
        stableId = sid.Id;
    }

    void OnEnable()
    {
        LoadOrInitState();
        EvaluateAvailabilityNow();

        if (DayNightSystem.Instance != null)
            DayNightSystem.Instance.OnDayAdvanced += OnDayAdvanced;
    }

    void OnDisable()
    {
        if (DayNightSystem.Instance != null)
            DayNightSystem.Instance.OnDayAdvanced -= OnDayAdvanced;
    }

    // ---- IInteractable ----
    public string GetPrompt()
    {
        if (!available) return "";
        var name = sampleDef ? sampleDef.displayName : "Unknown sample";
        return $"Pick up sample: {name} (+{amount})";
    }

    public void Interact(PlayerStats player)
    {
        TryGiveTo(player);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!pickupOnTrigger || !available) return;
        if (!other.CompareTag("Player")) return;
        TryGiveTo(other.GetComponent<PlayerStats>());
    }

    // ---- core ----
    void TryGiveTo(PlayerStats player)
    {
        if (!available || !player || !sampleDef) return;

        var inv = player.GetComponent<PlayerInventory>();
        if (!inv) return;

        inv.AddRockSample(sampleDef, Mathf.Max(1, amount));

        if (enableRespawn)
        {
            available = false;

            int today = (DayNightSystem.Instance != null)
                ? DayNightSystem.Instance.CurrentDay
                : (GameData.I != null ? GameData.I.CurrentDayMirror : 1);

            int min = Mathf.Max(1, respawnDaysMin);
            int max = Mathf.Max(min, respawnDaysMax);
            int delta = Random.Range(min, max + 1); // zárt intervallum
            nextAvailableDay = today + delta;

            ApplyVisual();
            SaveState();

            if (destroyOnPickup) Destroy(gameObject);
        }
        else
        {
            // nincs respawn → végleg töröljük és tisztítjuk a mentést
            if (GameData.I) GameData.I.ClearRockRespawn(stableId);
            Destroy(gameObject);
        }
    }

    void OnDayAdvanced(int newDay)
    {
        if (!available && newDay >= nextAvailableDay)
        {
            available = true;
            nextAvailableDay = int.MinValue;
            ApplyVisual();
            SaveState();
        }
    }

    void EvaluateAvailabilityNow()
    {
        int today = (DayNightSystem.Instance != null)
            ? DayNightSystem.Instance.CurrentDay
            : (GameData.I != null ? GameData.I.CurrentDayMirror : 1);

        if (!available && today >= nextAvailableDay)
        {
            available = true;
            nextAvailableDay = int.MinValue;
            ApplyVisual();
            SaveState();
        }
        else
        {
            ApplyVisual();
        }
    }

    void LoadOrInitState()
    {
        available = true;
        nextAvailableDay = int.MinValue;

        // GameData-s mentés
        if (GameData.I != null && GameData.I.TryGetRockRespawn(stableId, out var avail, out var nextDay))
        {
            available = avail;
            nextAvailableDay = nextDay;
        }
        else
        {
            if (enableRespawn && startOnCooldown)
            {
                int today = (DayNightSystem.Instance != null)
                    ? DayNightSystem.Instance.CurrentDay
                    : (GameData.I != null ? GameData.I.CurrentDayMirror : 1);

                int min = Mathf.Max(1, respawnDaysMin);
                int max = Mathf.Max(min, respawnDaysMax);
                int delta = Random.Range(min, max + 1);
                available = false;
                nextAvailableDay = today + delta;
            }
            SaveState(); // első init is kerüljön mentésbe
        }
    }

    void SaveState()
    {
        if (GameData.I != null)
            GameData.I.WriteRockRespawn(stableId, available, nextAvailableDay);
    }

    void ApplyVisual()
    {
        if (sr) sr.enabled = available;
        if (col) col.enabled = available;
    }
}
