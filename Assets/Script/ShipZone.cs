using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ShipZone : MonoBehaviour
{
    public float o2RechargePerSec = 15f;
    //public float energyRechargePerSec = 10f;

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var stats = other.GetComponent<PlayerStats>();
        if (!stats) return;

        stats.oxygen = Mathf.Min(100f, stats.oxygen + o2RechargePerSec * Time.deltaTime);
        //stats.energy = Mathf.Min(100f, stats.energy + energyRechargePerSec * Time.deltaTime);
    }
}
