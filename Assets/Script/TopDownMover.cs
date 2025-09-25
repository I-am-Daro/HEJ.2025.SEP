using System;
using UnityEngine;
using UnityEngine.Audio;          // <<< ÚJ
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownMover : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float defaultMoveSpeed = 4f;
    public float MoveSpeed { get; set; }

    [Header("Animation (normal gravity)")]
    [SerializeField] Animator animator;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] string isMovingParam = "IsMoving";
    [SerializeField] string speedParam = "Speed";

    [Header("Animation (Zero-G)")]
    [SerializeField] bool useZeroGParams = true;
    [SerializeField] string isZeroGParam = "IsZeroG";
    [SerializeField] string zgIsMovingParam = "ZG_IsMoving";
    [SerializeField] string zgSpeedParam = "ZG_Speed";
    [SerializeField, Range(0.0f, 0.2f)] float zgInputDeadzone = 0.02f;

    [Header("Footsteps (ground)")]
    [SerializeField] AudioSource footstepSource;
    [SerializeField] AudioClip[] footstepClips;
    [SerializeField, Range(0.1f, 1.0f)] float stepIntervalBase = 0.45f;
    [SerializeField] float stepPitchMin = 0.95f;
    [SerializeField] float stepPitchMax = 1.08f;
    [SerializeField] bool randomizeStartTime = true;
    [SerializeField] float minVelocityForStep = 0.2f;
    [SerializeField, Range(0f, 1f)] float groundSfxVolume = 1f;

    [Header("Movement SFX (Zero-G)")]
    [SerializeField] AudioSource zeroGSource;
    [SerializeField] AudioClip[] zeroGClips;
    [SerializeField, Range(0.1f, 1.2f)] float zgIntervalBase = 0.55f;
    [SerializeField] float zgPitchMin = 0.92f;
    [SerializeField] float zgPitchMax = 1.10f;
    [SerializeField] bool zgRandomizeStartTime = true;
    [SerializeField] float minVelocityForZeroGSound = 0.15f;
    [SerializeField, Range(0f, 1f)] float zeroGSfxVolume = 0.7f;

    [Header("Audio Routing")]
    [Tooltip("Kösd ide a Mixer SFX csoportját, hogy az Options SFX csúszka szabályozza ezeket a hangokat.")]
    [SerializeField] AudioMixerGroup sfxBus;   // <<< ÚJ

    [Header("Top-Down directional (Animator)")]
    [SerializeField] bool enableDirectionalTopDown = true;
    [SerializeField] bool directionalAlsoInZeroG = true;
    [SerializeField] string dirParam = "MoveDir";   // 0=Side, 1=Up, 2=Down
    [SerializeField, Range(0.5f, 0.95f)] float pureYThreshold = 0.75f;
    [SerializeField, Range(0f, 0.2f)] float inputDeadzoneTD = 0.05f;
    [SerializeField] bool preferInputOverVelocityTD = true;

    [Header("Sprite flip control")]
    [SerializeField, Range(0f, 0.2f)] float flipDeadzone = 0.05f;
    bool facingRight = true;

    int lastMoveDir = 0; // 0=Side, 1=Up, 2=Down

    Rigidbody2D rb;
    Vector2 moveInput;
    PlayerStats stats;

    float stepTimer, zgTimer;
    int lastStepIdx = -1, lastZgIdx = -1;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        MoveSpeed = defaultMoveSpeed;
        stats = GetComponent<PlayerStats>();

        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (!animator) animator = GetComponentInChildren<Animator>(true);

        // Ground footsteps forrás
        if (!footstepSource)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false; footstepSource.loop = false;
            footstepSource.spatialBlend = 0f;
        }
        footstepSource.volume = groundSfxVolume;

        // ZeroG whoosh forrás
        if (!zeroGSource)
        {
            zeroGSource = gameObject.AddComponent<AudioSource>();
            zeroGSource.playOnAwake = false; zeroGSource.loop = false;
            zeroGSource.spatialBlend = 0f;
        }
        zeroGSource.volume = zeroGSfxVolume;

        // <<< ÚJ: mindkét forrás rá a Mixer SFX csoportra >>>
        ApplyMixerGroup();
    }

    void ApplyMixerGroup()
    {
        if (sfxBus)
        {
            if (footstepSource) footstepSource.outputAudioMixerGroup = sfxBus;
            if (zeroGSource) zeroGSource.outputAudioMixerGroup = sfxBus;
        }
    }

    void OnValidate()
    {
        // Ha inspectorban később húzod be a sfxBus-t, érvényesítsük
        ApplyMixerGroup();
    }

    void OnDisable()
    {
        if (footstepSource) footstepSource.Stop();
        if (zeroGSource) zeroGSource.Stop();
        stepTimer = 0f; zgTimer = 0f;
    }

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();

    void FixedUpdate()
    {
        float mult = (stats != null) ? stats.MoveSpeedMultiplier : 1f;
#if UNITY_2022_3_OR_NEWER
        rb.linearVelocity = moveInput * (MoveSpeed * mult);
#else
        rb.velocity = moveInput * (MoveSpeed * mult);
#endif
    }

    void Update()
    {
#if UNITY_2022_3_OR_NEWER
        Vector2 v = rb.linearVelocity;
#else
        Vector2 v = rb.velocity;
#endif
        float speed = v.magnitude;
        bool zeroG = IsZeroG();

        UpdateAnimation(speed);
        HandleMovementAudio(speed, zeroG);

        // runtime volume finomhang
        if (footstepSource) footstepSource.volume = groundSfxVolume;
        if (zeroGSource) zeroGSource.volume = zeroGSfxVolume;

        // ha futás közben állítod be a bus-t, frissítsük
        if (sfxBus)
        {
            if (footstepSource && footstepSource.outputAudioMixerGroup != sfxBus)
                footstepSource.outputAudioMixerGroup = sfxBus;
            if (zeroGSource && zeroGSource.outputAudioMixerGroup != sfxBus)
                zeroGSource.outputAudioMixerGroup = sfxBus;
        }
    }

    bool IsZeroG() => (stats != null && stats.isZeroG);

    void UpdateAnimation(float currentSpeed)
    {
        bool zeroG = IsZeroG();

        bool movingZeroG = moveInput.sqrMagnitude >= (zgInputDeadzone * zgInputDeadzone);
        bool movingGround = currentSpeed >= 0.01f;
        bool moving = zeroG ? movingZeroG : movingGround;

        if (animator)
        {
            if (useZeroGParams && !string.IsNullOrEmpty(isZeroGParam))
                animator.SetBool(isZeroGParam, zeroG);

            if (!string.IsNullOrEmpty(isMovingParam)) animator.SetBool(isMovingParam, moving);
            if (!string.IsNullOrEmpty(speedParam)) animator.SetFloat(speedParam, currentSpeed);

            if (useZeroGParams)
            {
                if (!string.IsNullOrEmpty(zgIsMovingParam)) animator.SetBool(zgIsMovingParam, zeroG ? moving : false);
                if (!string.IsNullOrEmpty(zgSpeedParam)) animator.SetFloat(zgSpeedParam, zeroG ? currentSpeed : 0f);
            }
        }

        // Irány (ZeroG-ben is, ha engedélyezve)
        bool allowDirectional = enableDirectionalTopDown && (!zeroG || directionalAlsoInZeroG);
        int moveDir = lastMoveDir;

        if (allowDirectional && animator && !string.IsNullOrEmpty(dirParam))
        {
            Vector2 src = Vector2.zero;
            bool haveInput = moveInput.sqrMagnitude >= (inputDeadzoneTD * inputDeadzoneTD);

            if (preferInputOverVelocityTD && haveInput)
                src = moveInput.normalized;
            else
            {
#if UNITY_2022_3_OR_NEWER
                Vector2 vNow = rb.linearVelocity;
#else
                Vector2 vNow = rb.velocity;
#endif
                src = vNow.sqrMagnitude > 0.0001f ? vNow.normalized : Vector2.zero;
            }

            if (moving)
            {
                float ax = Mathf.Abs(src.x);
                float ay = Mathf.Abs(src.y);

                if (ay >= pureYThreshold * (ax + 1e-4f))
                    moveDir = (src.y >= 0f) ? 1 : 2; // Up / Down
                else
                    moveDir = 0; // Side

                lastMoveDir = moveDir;
            }

            animator.SetInteger(dirParam, moveDir);
        }

        // FlipX: horizontális szándék alapján (input előnyben)
        float horizSource;
        bool haveInputX = Mathf.Abs(moveInput.x) >= flipDeadzone;
        if (preferInputOverVelocityTD && haveInputX)
        {
            horizSource = moveInput.x;
        }
        else
        {
#if UNITY_2022_3_OR_NEWER
            horizSource = rb.linearVelocity.x;
#else
            horizSource = rb.velocity.x;
#endif
        }

        if (spriteRenderer && Mathf.Abs(horizSource) >= flipDeadzone)
        {
            spriteRenderer.flipX = horizSource < 0f;
            facingRight = !spriteRenderer.flipX;
        }
    }

    void HandleMovementAudio(float currentSpeed, bool zeroG)
    {
        if (zeroG)
        {
            if (footstepSource && footstepSource.isPlaying) footstepSource.Stop();
            stepTimer = 0f;
            HandleZeroGWhoosh(currentSpeed);
        }
        else
        {
            if (zeroGSource && zeroGSource.isPlaying) zeroGSource.Stop();
            zgTimer = 0f;
            HandleFootstepsGround(currentSpeed);
        }
    }

    void HandleFootstepsGround(float currentSpeed)
    {
        if (footstepClips == null || footstepClips.Length == 0 || footstepSource == null)
            return;

        bool animSaysMoving = animator && !string.IsNullOrEmpty(isMovingParam) && animator.GetBool(isMovingParam);
        bool movingFallback = (animator == null || string.IsNullOrEmpty(isMovingParam)) &&
                              currentSpeed >= minVelocityForStep;

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
            PlayIndexed(footstepSource, footstepClips, ref lastStepIdx, stepPitchMin, stepPitchMax, randomizeStartTime);
            stepTimer = stepIntervalBase;
        }
    }

    void HandleZeroGWhoosh(float currentSpeed)
    {
        if (zeroGClips == null || zeroGClips.Length == 0 || zeroGSource == null)
            return;

        bool moving = moveInput.sqrMagnitude >= (zgInputDeadzone * zgInputDeadzone);

        if (!moving)
        {
            zgTimer = 0f;
            if (zeroGSource.isPlaying) zeroGSource.Stop();
            return;
        }

        float speedNorm = Mathf.Clamp(currentSpeed / Mathf.Max(0.001f, MoveSpeed), 0.1f, 3f);
        zgTimer -= Time.deltaTime * speedNorm;

        if (zgTimer <= 0f)
        {
            PlayIndexed(zeroGSource, zeroGClips, ref lastZgIdx, zgPitchMin, zgPitchMax, zgRandomizeStartTime);
            zgTimer = zgIntervalBase;
        }
    }

    void PlayIndexed(AudioSource src, AudioClip[] pool, ref int lastIdx, float pMin, float pMax, bool randomStart)
    {
        if (pool == null || pool.Length == 0 || src == null) return;

        int idx;
        if (pool.Length == 1) idx = 0;
        else { do { idx = Random.Range(0, pool.Length); } while (idx == lastIdx); }
        lastIdx = idx;

        var clip = pool[idx];
        src.pitch = Random.Range(pMin, pMax);

        if (randomStart)
        {
            src.clip = clip;
            src.time = Mathf.Clamp(Random.Range(0f, clip.length * 0.7f), 0f, Mathf.Max(0f, clip.length - 0.01f));
            src.Play();
        }
        else
        {
            src.PlayOneShot(clip);
        }
    }
}
