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

    MoveMode currentMode;

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        topDown = GetComponent<TopDownMover>();
        side = GetComponent<PlatformerController2D>();
    }

    public void Apply(MoveMode mode, float? speedOverride = null, float? gravityOverride = null)
    {
        currentMode = mode;

        // mód váltás: melyik script aktív
        bool top = mode == MoveMode.ExteriorTopDown || mode == MoveMode.ZeroG;
        if (topDown) topDown.enabled = top;
        if (side) side.enabled = !top;

        // fizika
        float g = gravityOverride ?? (mode switch
        {
            MoveMode.InteriorSide => interiorGravity,
            MoveMode.ZeroG => zeroGGravity,
            _ => exteriorGravity
        });
        rb.gravityScale = g;
        rb.freezeRotation = true;

        // sebesség
        float spd = speedOverride ?? (mode switch
        {
            MoveMode.InteriorSide => interiorSpeed,
            MoveMode.ZeroG => zeroGSpeed,
            _ => exteriorSpeed
        });

        if (topDown) topDown.MoveSpeed = spd;
        if (side) side.MoveSpeed = spd;
    }

    // kényelmi API-k
    public void SetSpeed(float newSpeed)
    {
        if (topDown && topDown.enabled) topDown.MoveSpeed = newSpeed;
        if (side && side.enabled) side.MoveSpeed = newSpeed;
    }

    public void SetGravity(float newG) => rb.gravityScale = newG;

    public MoveMode CurrentMode => currentMode;
}
