using UnityEngine;

public enum MoveMode { ExteriorTopDown, InteriorSide, ZeroG }

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovementService : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] TopDownMover topDown;
    [SerializeField] PlatformerController2D side;
    [SerializeField] Rigidbody2D rb;

    [Header("Alap presetek")]
    public float exteriorSpeed = 4f;   // top-down
    public float interiorSpeed = 5f;   // side
    public float zeroGSpeed = 6f;    // jetpack-szerû

    public float interiorGravity = 2f;
    public float exteriorGravity = 0f;
    public float zeroGGravity = 0f;

    [Header("Speed multiplier (global)")]
    [Range(0f, 3f)] public float speedMultiplier = 1f;

    MoveMode currentMode;

    void Awake() 
    { 
        //GetComponent<UnityEngine.InputSystem.PlayerInput>()?.neverAutoSwitchControlSchemes = true; 
    }

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        topDown = GetComponent<TopDownMover>();
        side = GetComponent<PlatformerController2D>();
    }

    public void Apply(MoveMode mode, float? speedOverride = null, float? gravityOverride = null)
    {
        currentMode = mode;

        bool top = mode == MoveMode.ExteriorTopDown || mode == MoveMode.ZeroG;
        if (topDown) topDown.enabled = top;
        if (side) side.enabled = !top;

        float g = gravityOverride ?? (mode switch
        {
            MoveMode.InteriorSide => interiorGravity,
            MoveMode.ZeroG => zeroGGravity,
            _ => exteriorGravity
        });
        rb.gravityScale = g;
        rb.freezeRotation = true;

        // alap sebesség (vagy override), majd szorozzuk a multiplierrel
        float baseSpd = speedOverride ?? (mode switch
        {
            MoveMode.InteriorSide => interiorSpeed,
            MoveMode.ZeroG => zeroGSpeed,
            _ => exteriorSpeed
        });
        float finalSpd = baseSpd * Mathf.Max(0f, speedMultiplier);

        if (topDown) topDown.MoveSpeed = finalSpd;
        if (side) side.MoveSpeed = finalSpd;
    }

    public void SetSpeedMultiplier(float m)
    {
        speedMultiplier = Mathf.Max(0f, m);
        // re-apply current mode, hogy azonnal érvényesüljön
        Apply(currentMode);
    }

    // kényelmi API-k
    public void SetSpeed(float newBaseSpeed)
    {
        float final = newBaseSpeed * Mathf.Max(0f, speedMultiplier);
        if (topDown && topDown.enabled) topDown.MoveSpeed = final;
        if (side && side.enabled) side.MoveSpeed = final;
    }

    public void SetGravity(float newG) => rb.gravityScale = newG;

    public MoveMode CurrentMode => currentMode;
    public void ApplyExterior() => Apply(MoveMode.ExteriorTopDown);
    public void ApplyInterior() => Apply(MoveMode.InteriorSide);
    public void ApplyZeroG() => Apply(MoveMode.ZeroG);

}
