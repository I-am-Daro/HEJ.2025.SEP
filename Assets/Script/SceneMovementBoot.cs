using UnityEngine;

public enum SceneKind { Exterior, Interior }

public class SceneMovementBoot : MonoBehaviour
{
    [SerializeField] SceneKind sceneKind = SceneKind.Exterior;

    [Header("Player Scale")]
    [SerializeField] Vector3 exteriorScale = new Vector3(1f, 1f, 1f);
    [SerializeField] Vector3 interiorScale = new Vector3(2f, 2f, 1f);

    void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;

        var move = player.GetComponent<PlayerMovementService>();
        if (!move) return;

        // A jelenet megmondja, mi engedett:
        move.SetSceneAuthority(sceneKind);

        if (sceneKind == SceneKind.Interior)
        {
            move.Apply(MoveMode.InteriorSide);
            player.transform.localScale = interiorScale;
        }
        else
        {
            // Exterior: top-down (ZeroG z�na majd fel�l�rhatja ZeroG-re, ami engedett)
            move.Apply(MoveMode.ExteriorTopDown);
            player.transform.localScale = exteriorScale;
        }
    }
}
