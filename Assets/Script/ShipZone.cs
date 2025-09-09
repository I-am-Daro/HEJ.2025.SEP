using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ShipZone : MonoBehaviour
{
    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var stats = other.GetComponent<PlayerStats>();
        if (stats) stats.isInShipInterior = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var stats = other.GetComponent<PlayerStats>();
        if (stats) stats.isInShipInterior = false;
    }
}
