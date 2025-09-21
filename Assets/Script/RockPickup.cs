using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RockPickup : MonoBehaviour, IInteractable
{
    [Header("Rock Sample")]
    public RockSampleDefinition sampleDef;
    [Min(1)] public int amount = 1;

    [Header("Pickup behaviour")]
    public bool destroyOnPickup = true;
    public bool pickupOnTrigger = false;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    public string GetPrompt()
    {
        var name = sampleDef ? sampleDef.displayName : "Unknown sample";
        return $"Pick up sample: {name} (+{amount})";
    }

    public void Interact(PlayerStats player)
    {
        TryGiveTo(player);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!pickupOnTrigger) return;
        if (!other.CompareTag("Player")) return;
        TryGiveTo(other.GetComponent<PlayerStats>());
    }

    void TryGiveTo(PlayerStats player)
    {
        if (!player || !sampleDef) return;
        var inv = player.GetComponent<PlayerInventory>();
        if (!inv) return;

        inv.AddRockSample(sampleDef, Mathf.Max(1, amount));

        if (destroyOnPickup) Destroy(gameObject);
    }
}
