using System;
using UnityEngine;
using Random = UnityEngine.Random;


[RequireComponent(typeof(Collider2D))]
public class IronPickup : MonoBehaviour, IInteractable
{
    [Header("Pickup")]
    public int amount = 1;
    public bool destroyOnPickup = false; // respawn miatt default: ne destroy-oljuk

    [Header("Respawn (persistent)")]
    public bool enableRespawn = true;
    public int respawnDaysMin = 1;
    public int respawnDaysMax = 3;
    public bool startOnCooldown = false; // ha igaz: első belépéskor is várunk

    [Header("Auto-pickup (optional)")]
    public bool pickupOnTrigger = false;

    // cached
    SpriteRenderer sr;
    Collider2D col;
    string stableId;

    // lokális tükrözés a kényelem kedvéért
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

        // StableId (ajánlott), fallback egy determinisztikus kulcsra
        var sid = GetComponent<StableId>();
        if (!sid) sid = gameObject.AddComponent<StableId>(); // ha hiányzik, tegyünk fel
        if (string.IsNullOrEmpty(sid.Id)) sid.AssignNewRuntimeId();
        stableId = sid.Id;
    }

    void OnEnable()
    {
        // 1) állapot betöltése
        LoadOrInitState();

        // 2) belépéskor AZONNAL ellenőrizzük, lejárt-e a cooldown
        EvaluateAvailabilityNow();

        // 3) iratkozzunk fel a napváltásra
        if (DayNightSystem.Instance != null)
            DayNightSystem.Instance.OnDayAdvanced += OnDayAdvanced;
    }

    void OnDisable()
    {
        if (DayNightSystem.Instance != null)
            DayNightSystem.Instance.OnDayAdvanced -= OnDayAdvanced;
    }

    // ---- IInteractable ----
    public string GetPrompt() => available ? $"Pick up Iron (+{amount})" : "";

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

    // ---- belső logika ----
    void TryGiveTo(PlayerStats player)
    {
        if (!available || !player) return;

        var inv = player.GetComponent<PlayerInventory>();
        if (!inv) return;

        inv.AddIron(Mathf.Max(1, amount));

        if (enableRespawn)
        {
            // elrejtés + következő nap beállítása
            available = false;
            int today = (DayNightSystem.Instance != null)
                ? DayNightSystem.Instance.CurrentDay
                : (GameData.I != null ? GameData.I.CurrentDayMirror : 1);

            int min = Mathf.Max(1, respawnDaysMin);
            int max = Mathf.Max(min, respawnDaysMax);
            int delta = Random.Range(min, max + 1);       // zárt intervallum
            nextAvailableDay = today + delta;

            ApplyVisual();
            SaveState();

            // ha mégis destroy-t szeretnél, itt lehet:
            if (destroyOnPickup) Destroy(gameObject);
        }
        else
        {
            // nincs respawn: egyszer használatos
            if (GameData.I) GameData.I.ClearIronRespawn(stableId);
            Destroy(gameObject);
        }
    }

    void OnDayAdvanced(int newDay)
    {
        // ha már eljött az ideje → újra elérhető
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

        if (GameData.I != null && GameData.I.TryGetIronRespawn(stableId, out var avail, out var nextDay))
        {
            available = avail;
            nextAvailableDay = nextDay;
            // Debug.Log($"[IronPickup:{name}] Loaded saved state: avail={available}, nextDay={nextAvailableDay}");
        }
        else
        {
            // első futás – ha startOnCooldown, tegyük várakozásba
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
            SaveState();
        }
    }

    void SaveState()
    {
        if (GameData.I != null)
            GameData.I.WriteIronRespawn(stableId, available, nextAvailableDay);
    }

    void ApplyVisual()
    {
        // csak a renderer/collider állapotát kapcsoljuk
        if (sr) sr.enabled = available;
        if (col) col.enabled = available;
    }
}
