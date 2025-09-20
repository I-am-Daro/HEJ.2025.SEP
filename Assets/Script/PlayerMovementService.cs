using UnityEngine;

public enum MoveMode { ExteriorTopDown, InteriorSide, ZeroG }
public enum SceneAuthority { None, Exterior, Interior }

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovementService : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] TopDownMover topDown;
    [SerializeField] PlatformerController2D side;
    [SerializeField] Rigidbody2D rb;

    [Header("Base speeds")]
    public float exteriorSpeed = 4f;   // top-down
    public float interiorSpeed = 5f;   // side
    public float zeroGSpeed = 6f;    // jetpack

    [Header("Gravity by mode")]
    public float interiorGravity = 2f;
    public float exteriorGravity = 0f;
    public float zeroGGravity = 0f;

    [Header("Speed multiplier (global)")]
    [Range(0f, 3f)] public float speedMultiplier = 1f;

    [Header("Debug")]
    [SerializeField] bool logApplies = false;

    MoveMode currentMode;
    SceneAuthority sceneAuthority = SceneAuthority.None;   // <<< �J

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        topDown = GetComponent<TopDownMover>();
        side = GetComponent<PlatformerController2D>();
    }


    /// K�zvetlen alapsebess�g �ll�t�s (a scene-aktu�lis komponensen).
    public void SetSpeed(float newBaseSpeed)
    {
        float final = Mathf.Max(0f, newBaseSpeed) * Mathf.Max(0f, speedMultiplier);
        if (topDown && topDown.enabled) topDown.MoveSpeed = final;
        if (side && side.enabled) side.MoveSpeed = final;
    }

    /// K�zvetlen gravit�ci� �ll�t�s (rigidbody-n).
    public void SetGravity(float newG)
    {
        if (rb) rb.gravityScale = newG;
    }

    /// A jelenet (SceneMovementBoot) �ll�tja be, hogy mi engedett.
    public void SetSceneAuthority(SceneKind kind)
    {
        sceneAuthority = (kind == SceneKind.Interior) ? SceneAuthority.Interior : SceneAuthority.Exterior;
    }

    /// F� bel�p�si pont � a sceneAuthority fel�lb�r�lja a nem odaill� m�dot.
    public void Apply(MoveMode requested, float? speedOverride = null, float? gravityOverride = null)
    {
        // K�nyszer�t�s jelenet szerint:
        if (sceneAuthority == SceneAuthority.Exterior)
        {
            // Exteriorban csak ExteriorTopDown vagy ZeroG engedett
            if (requested == MoveMode.InteriorSide)
                requested = MoveMode.ExteriorTopDown;
        }
        else if (sceneAuthority == SceneAuthority.Interior)
        {
            // Interiorban mindig side legyen (ha m�gis ZeroG-t k�rne valami, itt d�ntheted el mit tegyen)
            if (requested != MoveMode.InteriorSide)
                requested = MoveMode.InteriorSide;
        }

        currentMode = requested;

        bool useTopDown = (requested == MoveMode.ExteriorTopDown || requested == MoveMode.ZeroG);
        if (topDown) topDown.enabled = useTopDown;
        if (side) side.enabled = !useTopDown;

        float g = gravityOverride ?? (requested switch
        {
            MoveMode.InteriorSide => interiorGravity,
            MoveMode.ZeroG => zeroGGravity,
            _ => exteriorGravity
        });
        if (rb)
        {
            rb.gravityScale = g;
            rb.freezeRotation = true;
        }

        float baseSpd = speedOverride ?? (requested switch
        {
            MoveMode.InteriorSide => interiorSpeed,
            MoveMode.ZeroG => zeroGSpeed,
            _ => exteriorSpeed
        });
        float finalSpd = baseSpd * Mathf.Max(0f, speedMultiplier);

        if (useTopDown && topDown) topDown.MoveSpeed = finalSpd;
        if (!useTopDown && side) side.MoveSpeed = finalSpd;

        if (logApplies)
            Debug.Log($"[PlayerMovementService] Apply -> {requested} | sceneAuthority={sceneAuthority} | v={finalSpd} g={g}");
    }

    public void SetSpeedMultiplier(float m)
    {
        speedMultiplier = Mathf.Max(0f, m);
        Apply(currentMode); // friss�ts�k azonnal
    }

    // K�nyelmi h�v�sok
    public MoveMode CurrentMode => currentMode;
    public void ApplyExterior() => Apply(MoveMode.ExteriorTopDown);
    public void ApplyInterior() => Apply(MoveMode.InteriorSide);
    public void ApplyZeroG() => Apply(MoveMode.ZeroG);
}
