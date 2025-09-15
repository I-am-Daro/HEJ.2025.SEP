using UnityEngine;
using UnityEngine.InputSystem;


public class SceneMovementEnforcer : MonoBehaviour
{
    [Header("Melyik típusú scene ez?")]
    [SerializeField] SceneMovementKind sceneKind = SceneMovementKind.Exterior;

    [Header("Player referenciák (opcionális, auto-find ha üres)")]
    [SerializeField] Transform playerRoot;

    PlayerMovementService mover;
    PlayerStats stats;
    PlayerInput playerInput;

    void Start()
    {
        // Player megtalálása
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

        // NE váltogasson control schemét UI miatt
        if (playerInput != null)
            playerInput.neverAutoSwitchControlSchemes = true;

        // elsõ beállítás
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

        // Exterior: ha ZeroG zónában van, akkor ZeroG, különben TopDown
        bool zero = stats != null && stats.isZeroG;
        return zero ? MoveMode.ZeroG : MoveMode.ExteriorTopDown;
    }
}
