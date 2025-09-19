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

        if (sceneKind == SceneKind.Interior)
        {
            move.Apply(MoveMode.InteriorSide);
            player.transform.localScale = interiorScale;  // nagyobb belül
        }
        else
        {
            move.Apply(MoveMode.ExteriorTopDown);
            player.transform.localScale = exteriorScale;  // kisebb kívül
        }
    }
}
