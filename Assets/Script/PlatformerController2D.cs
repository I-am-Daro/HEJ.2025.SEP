using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody2D))]
public class PlatformerController2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float defaultMoveSpeed = 5f;
    public float MoveSpeed { get; set; }

    [Header("Animation")]
    [SerializeField] Animator animator;                 // PlayerSprite-en lévõ Animator
    [SerializeField] SpriteRenderer spriteRenderer;     // ugyanazon a GO-n vagy childon
    [SerializeField] string isMovingParam = "IsMoving"; // bool
    [SerializeField] string speedParam = "Speed";       // float (opcionális)

    [Header("Zero-G Animator Reset (when interior)")]
    [Tooltip("Ha igaz: amikor ez a controller aktív, minden frame-ben reseteljük a Zero-G paramokat.")]
    [SerializeField] bool resetZeroGAnimator = true;
    [SerializeField] string isZeroGParam = "IsZeroG";      // bool
    [SerializeField] string zgIsMovingParam = "ZG_IsMoving"; // bool
    [SerializeField] string zgSpeedParam = "ZG_Speed";       // float

    [Header("Ground check (optional)")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundCheckRadius = 0.08f;
    [SerializeField] LayerMask groundMask = ~0;

    [Header("Footsteps (INTERIOR)")]
    [SerializeField] AudioSource footstepSource;
    [SerializeField] AudioClip[] footstepClips;
    [SerializeField, Range(0.1f, 1.0f)] float stepIntervalBase = 0.42f;
    [SerializeField] float stepPitchMin = 0.95f;
    [SerializeField] float stepPitchMax = 1.08f;
    [SerializeField] bool randomizeStartTime = true;
    [SerializeField] float minHorizontalSpeedForStep = 0.15f;

    Rigidbody2D rb;
    float moveX;
    PlayerStats stats;

    float stepTimer;
    int lastClipIndex = -1;

    // animator param cache
    bool hasIsMovingParam;
    bool hasSpeedParam;
    bool hasIsZeroGParam;
    bool hasZgIsMovingParam;
    bool hasZgSpeedParam;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        MoveSpeed = defaultMoveSpeed;
        stats = GetComponent<PlayerStats>();

        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (!animator) animator = GetComponentInChildren<Animator>(true);

        if (animator)
        {
            hasIsMovingParam = !string.IsNullOrEmpty(isMovingParam) &&
                               AnimatorHasParam(animator, isMovingParam, AnimatorControllerParameterType.Bool);
            hasSpeedParam = !string.IsNullOrEmpty(speedParam) &&
                            AnimatorHasParam(animator, speedParam, AnimatorControllerParameterType.Float);

            // Zero-G paramokat is feljegyezzük, hogy csak akkor állítsuk, ha tényleg léteznek
            hasIsZeroGParam = !string.IsNullOrEmpty(isZeroGParam) &&
                              AnimatorHasParam(animator, isZeroGParam, AnimatorControllerParameterType.Bool);
            hasZgIsMovingParam = !string.IsNullOrEmpty(zgIsMovingParam) &&
                                 AnimatorHasParam(animator, zgIsMovingParam, AnimatorControllerParameterType.Bool);
            hasZgSpeedParam = !string.IsNullOrEmpty(zgSpeedParam) &&
                              AnimatorHasParam(animator, zgSpeedParam, AnimatorControllerParameterType.Float);
        }

        if (!footstepSource)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.loop = false;
            footstepSource.spatialBlend = 0f;
            footstepSource.volume = 1f;
        }
    }

    // <<< FONTOS: amikor a belsõ (platformer) controller ÉLES, kilépünk Zero-G-bõl és kinullázzuk a Zero-G anim paramokat >>>
    void OnEnable()
    {
        if (stats) stats.isZeroG = false;
        if (resetZeroGAnimator) ClearZeroGAnimatorState();
    }

    static bool AnimatorHasParam(Animator anim, string paramName, AnimatorControllerParameterType type)
    {
        foreach (var p in anim.parameters)
            if (p.type == type && p.name == paramName) return true;
        return false;
    }

    void OnDisable()
    {
        if (footstepSource) footstepSource.Stop();
        stepTimer = 0f;
    }

    public void OnMove(InputValue value)
    {
        Vector2 v = value.Get<Vector2>();
        moveX = v.x;
    }

    void FixedUpdate()
    {
        float mult = (stats != null) ? stats.MoveSpeedMultiplier : 1f;
#if UNITY_2022_3_OR_NEWER
        rb.linearVelocity = new Vector2(moveX * (MoveSpeed * mult), rb.linearVelocity.y);
#else
        rb.velocity = new Vector2(moveX * (MoveSpeed * mult), rb.velocity.y);
#endif
    }

    void Update()
    {
        // Biztonsági háló: amíg belül vagyunk, a Zero-G paramok minden frame-ben NULLÁZVA maradnak
        if (resetZeroGAnimator) ClearZeroGAnimatorState();

#if UNITY_2022_3_OR_NEWER
        float absVX = Mathf.Abs(rb.linearVelocity.x);
        float vx = rb.linearVelocity.x;
#else
        float absVX = Mathf.Abs(rb.velocity.x);
        float vx = rb.velocity.x;
#endif
        bool grounded = IsGrounded();

        UpdateAnimation(absVX, vx);
        HandleFootsteps(absVX, grounded);
    }

    void ClearZeroGAnimatorState()
    {
        if (!animator) return;
        if (hasIsZeroGParam) animator.SetBool(isZeroGParam, false);
        if (hasZgIsMovingParam) animator.SetBool(zgIsMovingParam, false);
        if (hasZgSpeedParam) animator.SetFloat(zgSpeedParam, 0f);
    }

    bool IsGrounded()
    {
        if (!groundCheck) return true;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);
    }

    void UpdateAnimation(float absVX, float vx)
    {
        bool moving = absVX >= 0.01f;

        if (animator)
        {
            if (hasIsMovingParam) animator.SetBool(isMovingParam, moving);
            if (hasSpeedParam) animator.SetFloat(speedParam, absVX);
        }

        if (spriteRenderer && Mathf.Abs(vx) > 0.01f)
            spriteRenderer.flipX = vx < 0f;
    }

    void HandleFootsteps(float horizontalSpeed, bool grounded)
    {
        if (footstepClips == null || footstepClips.Length == 0 || footstepSource == null)
            return;

        bool animSaysMoving = animator && hasIsMovingParam && animator.GetBool(isMovingParam);
        bool movingFallback = (!animator || !hasIsMovingParam) &&
                              grounded && horizontalSpeed >= minHorizontalSpeedForStep;

        bool shouldStep = (animSaysMoving && grounded) || movingFallback;
        if (!shouldStep)
        {
            stepTimer = 0f;
            if (footstepSource.isPlaying) footstepSource.Stop();
            return;
        }

        float speedNorm = Mathf.Clamp(horizontalSpeed / Mathf.Max(0.001f, MoveSpeed), 0.1f, 3f);
        stepTimer -= Time.deltaTime * speedNorm;

        if (stepTimer <= 0f)
        {
            PlayFootstep();
            stepTimer = stepIntervalBase;
        }
    }

    void PlayFootstep()
    {
        int idx;
        if (footstepClips.Length == 1) idx = 0;
        else { do { idx = Random.Range(0, footstepClips.Length); } while (idx == lastClipIndex); }
        lastClipIndex = idx;

        var clip = footstepClips[idx];
        footstepSource.pitch = Random.Range(stepPitchMin, stepPitchMax);

        if (randomizeStartTime)
        {
            footstepSource.clip = clip;
            footstepSource.time = Random.Range(0f, clip.length * 0.7f);
            footstepSource.Play();
        }
        else
        {
            footstepSource.PlayOneShot(clip);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
#endif
}
