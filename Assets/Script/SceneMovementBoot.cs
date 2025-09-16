using UnityEngine;

public enum SceneKind { Exterior, Interior }

public class SceneMovementBoot : MonoBehaviour
{
    [SerializeField] SceneKind sceneKind = SceneKind.Exterior;

    void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;

        var move = player.GetComponent<PlayerMovementService>();
        if (!move) return;

        if (sceneKind == SceneKind.Interior)
        {
            move.Apply(MoveMode.InteriorSide);
        }
        else
        {
            // Exterior: induljunk top-downban; ZeroG zóna majd felülírja
            move.Apply(MoveMode.ExteriorTopDown);
        }
    }
}
