using UnityEngine;


public class ZeroGBootstrap : MonoBehaviour
{
    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (!p) return;
        var stats = p.GetComponent<PlayerStats>();
        var move = p.GetComponent<PlayerMovementService>();
        if (stats) stats.isZeroG = true;          // baseline: ZeroG mindenhol
        if (move) move.Apply(MoveMode.ZeroG);    // kivéve ahol buborék van
    }
}
