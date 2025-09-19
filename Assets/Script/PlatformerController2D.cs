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

    [Header("Ground check (optional)")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundCheckRadius = 0.08f;
    [SerializeField] LayerMask groundMask = ~0;

    [Header("Footsteps (INTERIOR)")]
    [SerializeField] AudioSource footstepSource;
    [SerializeField] AudioClip[] footstepClips;         // beltéri/lépcsõ/fém stb.
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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        MoveSpeed = defaultMoveSpeed;
        stats = GetComponent<PlayerStats>();

        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (!animator) animator = GetComponentInChildren<Animator>(true);

        if (!footstepSource)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.loop = false;
            footstepSource.spatialBlend = 0f;
            footstepSource.volume = 1f;
        }
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
        rb.linearVelocity = new Vector2(moveX * (MoveSpeed * mult), rb.linearVelocity.y);
    }

    void Update()
    {
        float absVX = Mathf.Abs(rb.linearVelocity.x);
        bool grounded = IsGrounded();

        UpdateAnimation(absVX, rb.linearVelocity.x);
        HandleFootsteps(absVX, grounded);
    }

    bool IsGrounded()
    {
        if (!groundCheck) return true; // ha nincs megadva, ne némítsuk emiatt
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);
    }

    void UpdateAnimation(float absVX, float vx)
    {
        bool moving = absVX >= 0.01f;

        if (animator)
        {
            if (!string.IsNullOrEmpty(isMovingParam)) animator.SetBool(isMovingParam, moving);
            if (!string.IsNullOrEmpty(speedParam)) animator.SetFloat(speedParam, absVX);
        }

        if (spriteRenderer && Mathf.Abs(vx) > 0.01f)
            spriteRenderer.flipX = vx < 0f;
    }

    void HandleFootsteps(float horizontalSpeed, bool grounded)
    {
        if (footstepClips == null || footstepClips.Length == 0 || footstepSource == null)
            return;

        // 1) csak WALK anim alatt
        bool animSaysMoving = animator && !string.IsNullOrEmpty(isMovingParam) && animator.GetBool(isMovingParam);

        // 2) Talajon legyünk, és fallback ha nincs animator
        bool movingFallback = (animator == null || string.IsNullOrEmpty(isMovingParam)) &&
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
        else
        {
            do { idx = Random.Range(0, footstepClips.Length); }
            while (idx == lastClipIndex);
        }
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
