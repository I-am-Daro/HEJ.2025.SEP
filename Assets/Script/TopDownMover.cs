using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownMover : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float defaultMoveSpeed = 4f;
    public float MoveSpeed { get; set; }

    [Header("Animation")]
    [SerializeField] Animator animator;                 // PlayerSprite-en lévő Animator
    [SerializeField] SpriteRenderer spriteRenderer;     // ugyanazon a GO-n vagy childon
    [SerializeField] string isMovingParam = "IsMoving"; // bool
    [SerializeField] string speedParam = "Speed";       // float (opcionális)

    [Header("Footsteps (EXTERIOR)")]
    [SerializeField] AudioSource footstepSource;
    [SerializeField] AudioClip[] footstepClips;         // külső talaj hangok
    [SerializeField, Range(0.1f, 1.0f)] float stepIntervalBase = 0.45f;
    [SerializeField] float stepPitchMin = 0.95f;
    [SerializeField] float stepPitchMax = 1.08f;
    [SerializeField] bool randomizeStartTime = true;
    [SerializeField] float minVelocityForStep = 0.2f;   // sebesség fallback küszöb

    Rigidbody2D rb;
    Vector2 moveInput;
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
        // nehogy idle-ben tovább szóljon
        if (footstepSource) footstepSource.Stop();
        stepTimer = 0f;
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void FixedUpdate()
    {
        float mult = (stats != null) ? stats.MoveSpeedMultiplier : 1f;
        rb.linearVelocity = moveInput * (MoveSpeed * mult);
    }

    void Update()
    {
        float speed = rb.linearVelocity.magnitude;
        UpdateAnimation(speed, rb.linearVelocity.x);
        HandleFootsteps(speed);
    }

    void UpdateAnimation(float currentSpeed, float vx)
    {
        bool moving = currentSpeed >= 0.01f;

        if (animator)
        {
            if (!string.IsNullOrEmpty(isMovingParam)) animator.SetBool(isMovingParam, moving);
            if (!string.IsNullOrEmpty(speedParam)) animator.SetFloat(speedParam, currentSpeed);
        }

        // flip: jobbra alap → balra menet flipX = true
        if (spriteRenderer && Mathf.Abs(vx) > 0.01f)
            spriteRenderer.flipX = vx < 0f;
    }

    void HandleFootsteps(float currentSpeed)
    {
        if (footstepClips == null || footstepClips.Length == 0 || footstepSource == null)
            return;

        // 1) csak WALK anim alatt szólunk
        bool animSaysMoving = animator && !string.IsNullOrEmpty(isMovingParam) && animator.GetBool(isMovingParam);

        // 2) ha nincs animator vagy param, essünk vissza sebesség küszöbre
        bool movingFallback = (animator == null || string.IsNullOrEmpty(isMovingParam)) && currentSpeed >= minVelocityForStep;

        bool shouldStep = animSaysMoving || movingFallback;
        if (!shouldStep)
        {
            stepTimer = 0f;
            if (footstepSource.isPlaying) footstepSource.Stop();
            return;
        }

        float speedNorm = Mathf.Clamp(currentSpeed / Mathf.Max(0.001f, MoveSpeed), 0.1f, 3f);
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
}
