using UnityEngine;
using UnityEngine.InputSystem;


public class SceneMovementEnforcer : MonoBehaviour
{
    [Header("Melyik t�pus� scene ez?")]
    [SerializeField] SceneMovementKind sceneKind = SceneMovementKind.Exterior;

    [Header("Player referenci�k (opcion�lis, auto-find ha �res)")]
    [SerializeField] Transform playerRoot;

    PlayerMovementService mover;
    PlayerStats stats;
    PlayerInput playerInput;

    void Start()
    {
        // Player megtal�l�sa
        if (!playerRoot)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) playerRoot = p.transform;
        }

        if (!playerRoot)
        {
            Debug.LogWarning("[SceneMovementEnforcer] Player not found (tag=Player).");
            return;
        }

        mover = playerRoot.GetComponent<PlayerMovementService>();
        stats = playerRoot.GetComponent<PlayerStats>();
        playerInput = playerRoot.GetComponent<PlayerInput>();

        // NE v�ltogasson control schem�t UI miatt
        if (playerInput != null)
            playerInput.neverAutoSwitchControlSchemes = true;

        // els� be�ll�t�s
        ApplyOnce();
    }

    void LateUpdate()
    {
        if (!mover) return;

        var desired = DesiredMode();
        if (mover.CurrentMode != desired)
            mover.Apply(desired);
    }

    void ApplyOnce()
    {
        if (mover) mover.Apply(DesiredMode());
    }

    MoveMode DesiredMode()
    {
        if (sceneKind == SceneMovementKind.Interior)
            return MoveMode.InteriorSide;

        // Exterior: ha ZeroG z�n�ban van, akkor ZeroG, k�l�nben TopDown
        bool zero = stats != null && stats.isZeroG;
        return zero ? MoveMode.ZeroG : MoveMode.ExteriorTopDown;
    }
}
