using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ZeroGZone : MonoBehaviour
{
    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var stats = other.GetComponent<PlayerStats>();
        var move = other.GetComponent<PlayerMovementService>();
        if (stats) stats.isZeroG = true;
        if (move) move.Apply(MoveMode.ZeroG);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var stats = other.GetComponent<PlayerStats>();
        var move = other.GetComponent<PlayerMovementService>();
        if (stats) stats.isZeroG = false;
        if (move) move.Apply(MoveMode.ExteriorTopDown);
    }
}
