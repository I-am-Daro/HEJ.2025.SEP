using UnityEngine;

public class ForceInteriorMovement : MonoBehaviour
{
    void OnEnable()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;
        var svc = player.GetComponent<PlayerMovementService>();
        if (!svc) return;
        svc.Apply(MoveMode.InteriorSide); // gravity > 0, Platformer ON, TopDown OFF
    }
}
