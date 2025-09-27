using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// A locsolókanna kézbevételét és az öntözés anim+SFX logikát kezeli.
/// - Világméret fixálás kézben
/// - Flip-követés
/// - Öntözés animáció bool-lal
/// - Hang: Mixerbe kötve, időzítve, szükség esetén a klipnél rövidebbre vágva
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class WateringCan : MonoBehaviour
{
    [Header("Carry settings")]
    [Tooltip("A Player alatt létrehozott / megtalált kéz-ankor neve.")]
    public string handAnchorName = "HandAnchor";
    [Tooltip("Jobbra nézéskor használt lokális offset a kéz-ankorhoz képest.")]
    public Vector3 carryLocalOffset = new Vector3(0.2f, -0.05f, 0f);
    [Tooltip("Ha balra fordul a Player, tükrözzük-e az offset X-ét?")]
    public bool mirrorOffsetOnFlip = true;
    [Tooltip("Kézben tartva emeljük meg a SpriteRenderer-ek sorting orderét ennyivel.")]
    public int carrySortingOrderBoost = 10;

    [Header("Animation (can)")]
    [Tooltip("A kanna saját Animator-a (pl. vízsugár anim). Opcionális – ha üres, automatikusan megkeressük.")]
    public Animator animator;
    [Tooltip("Bool param neve, ami jelzi, hogy épp öntözünk-e.")]
    public string isWateringParam = "IsWatering";

    [Header("Audio")]
    [Tooltip("A kanna saját AudioSource-a (ha nincs, létrejön).")]
    public AudioSource wateringAudio;
    [Tooltip("Kimeneti Mixer csoport (SFX busz).")]
    public AudioMixerGroup outputMixerGroup;
    [Tooltip("Loopoló csobogás, amíg öntözünk.")]
    public AudioClip loopWaterClip;
    [Tooltip("Egyszeri spricc hang rövid öntözésnél.")]
    public AudioClip oneShotWaterClip;
    [Range(0f, 1f)] public float waterVolume = 0.9f;
    [Range(0.5f, 1.5f)] public float waterPitch = 1.0f;

    [Header("Audio duration limits (sec)")]
    [Tooltip("Ha >0, a loopoló csobogást ennyi másodpercnél tovább nem hagyjuk szólni (akkor sem, ha ON maradna).")]
    public float loopMaxDuration = 0f;
    [Tooltip("Ha >0, a one-shot spriccet legfeljebb eddig engedjük szólni (ha a klip hosszabb).")]
    public float oneShotMaxDuration = 0f;

    [Header("Sync to animation (optional)")]
    [Tooltip("Ha igaz, a hang időtartamát igyekszünk az animációhoz igazítani.")]
    public bool syncAudioToAnim = false;
    [Tooltip("Konkrét animációs klip az öntözéshez – ha nincs, a controllerből próbáljuk kitalálni.")]
    public AnimationClip wateringAnimClip;

    // --- state ---
    public bool IsCarried { get; private set; }
    public Transform CarrierAnchor { get; private set; }

    // --- cached ---
    Collider2D col;
    SpriteRenderer[] srs;
    int[] originalOrders;

    Transform originalParent;
    Vector3 originalLocalPos;
    Quaternion originalLocalRot;
    Vector3 originalLocalScale;

    Transform homeParent;
    Vector3 homeLocalPos;
    Quaternion homeLocalRot;
    Vector3 homeLocalScale = Vector3.one;

    SpriteRenderer carrierSprite;
    bool lastCarrierFlipX;
    Vector3 desiredWorldScale;

    Coroutine playLimiterCo;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;

        srs = GetComponentsInChildren<SpriteRenderer>(true);
        if (srs != null)
        {
            originalOrders = new int[srs.Length];
            for (int i = 0; i < srs.Length; i++)
                originalOrders[i] = srs[i].sortingOrder;
        }

        originalParent = transform.parent;
        originalLocalPos = transform.localPosition;
        originalLocalRot = transform.localRotation;
        originalLocalScale = transform.localScale;

        desiredWorldScale = transform.lossyScale;

        homeParent = originalParent;
        homeLocalPos = originalLocalPos;
        homeLocalRot = originalLocalRot;
        homeLocalScale = originalLocalScale;

        if (!animator) animator = GetComponentInChildren<Animator>(true);

        if (!wateringAudio)
        {
            wateringAudio = gameObject.AddComponent<AudioSource>();
            wateringAudio.playOnAwake = false;
            wateringAudio.loop = false;
            wateringAudio.spatialBlend = 0f;
        }
        wateringAudio.volume = waterVolume;
        wateringAudio.pitch = waterPitch;
        if (outputMixerGroup) wateringAudio.outputAudioMixerGroup = outputMixerGroup;
    }

    void OnValidate()
    {
        // Inspectorban változtatott értékek érvényesítése futás közben is
        if (wateringAudio)
        {
            wateringAudio.volume = Mathf.Clamp01(waterVolume);
            wateringAudio.pitch = Mathf.Clamp(waterPitch, 0.5f, 1.5f);
            if (outputMixerGroup && wateringAudio.outputAudioMixerGroup != outputMixerGroup)
                wateringAudio.outputAudioMixerGroup = outputMixerGroup;
        }
    }

    // Station hívja setuphoz
    public void ConfigureHome(Transform parent, Vector3 localPos, Quaternion localRot, Vector3 localScale)
    {
        homeParent = parent != null ? parent : originalParent;
        homeLocalPos = localPos;
        homeLocalRot = localRot;
        homeLocalScale = (localScale == Vector3.zero) ? Vector3.one : localScale;
    }

    Transform FindOrMakeHandAnchor(Transform root, string name)
    {
        var t = root.Find(name);
        if (t) return t;
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go.transform;
    }

    public void PickUp(PlayerStats player, PlayerInventory inv)
    {
        if (IsCarried || player == null || inv == null) return;
        if (!inv.TryTakeCan(this)) return;

        CarrierAnchor = FindOrMakeHandAnchor(player.transform, handAnchorName);
        CarrierAnchor.localScale = Vector3.one;

        carrierSprite = player.GetComponentInChildren<SpriteRenderer>(true);
        lastCarrierFlipX = carrierSprite ? carrierSprite.flipX : false;

        transform.SetParent(CarrierAnchor, false);
        transform.localPosition = carryLocalOffset;
        transform.localRotation = Quaternion.identity;

        ApplyCompensatedScale(flipX: lastCarrierFlipX);

        if (col) col.enabled = false;

        if (srs != null)
            for (int i = 0; i < srs.Length; i++)
                srs[i].sortingOrder = originalOrders[i] + carrySortingOrderBoost;

        IsCarried = true;
    }

    public void PutBackHome()
    {
        StopAllCoroutines();
        playLimiterCo = null;
        SetWatering(false);

        transform.SetParent(homeParent, false);
        transform.localPosition = homeLocalPos;
        transform.localRotation = homeLocalRot;
        transform.localScale = homeLocalScale;

        if (col) col.enabled = true;

        if (srs != null)
            for (int i = 0; i < srs.Length; i++)
                srs[i].sortingOrder = originalOrders[i];

        CarrierAnchor = null;
        carrierSprite = null;
        IsCarried = false;
    }

    void LateUpdate()
    {
        if (!IsCarried || CarrierAnchor == null) return;

        bool flip = carrierSprite ? carrierSprite.flipX : CarrierAnchor.lossyScale.x < 0f;
        if (flip != lastCarrierFlipX)
        {
            if (mirrorOffsetOnFlip)
            {
                Vector3 off = carryLocalOffset;
                off.x = Mathf.Abs(off.x) * (flip ? -1f : 1f);
                transform.localPosition = off;
            }
            ApplyCompensatedScale(flip);
            lastCarrierFlipX = flip;
        }
    }

    void ApplyCompensatedScale(bool flipX)
    {
        if (CarrierAnchor == null) return;

        Vector3 p = CarrierAnchor.lossyScale;
        float cx = Mathf.Approximately(p.x, 0f) ? 1f : p.x;
        float cy = Mathf.Approximately(p.y, 0f) ? 1f : p.y;

        float sx = desiredWorldScale.x / cx;
        float sy = desiredWorldScale.y / cy;

        transform.localScale = new Vector3(flipX ? -Mathf.Abs(sx) : Mathf.Abs(sx), Mathf.Abs(sy), 1f);
    }

    // ================= ANIM + AUDIO API =================

    /// <summary> Folyamatos öntözés on/off (pl. gomb tartás). </summary>
    public void SetWatering(bool on)
    {
        // ANIM
        if (animator && !string.IsNullOrEmpty(isWateringParam))
        {
            // csak akkor állítjuk, ha tényleg változott – ezzel kíméljük a controllert
            if (animator.GetBool(isWateringParam) != on)
                animator.SetBool(isWateringParam, on);
        }

        // AUDIO
        if (!wateringAudio) return;

        // mindig frissek legyenek a kézzel állított értékek
        wateringAudio.volume = waterVolume;
        wateringAudio.pitch = waterPitch;
        if (outputMixerGroup) wateringAudio.outputAudioMixerGroup = outputMixerGroup;

        if (on)
        {
            // ha volt korábbi limiter, állítsuk le
            if (playLimiterCo != null) { StopCoroutine(playLimiterCo); playLimiterCo = null; }

            if (loopWaterClip)
            {
                wateringAudio.loop = true;
                wateringAudio.clip = loopWaterClip;
                if (!wateringAudio.isPlaying) wateringAudio.Play();

                // ha kértél maximum hosszt VAGY anim szinkront, limitáljuk
                float limit = CalcDesiredDuration(loopMaxDuration);
                if (limit > 0f) playLimiterCo = StartCoroutine(StopAfter(limit));
            }
            else if (oneShotWaterClip)
            {
                wateringAudio.loop = false;

                // OneShot-nál nincs pitch-kompenzáció, ezért kézzel időzítünk
                float naturalDur = oneShotWaterClip.length / Mathf.Max(0.01f, wateringAudio.pitch);
                float forced = CalcDesiredDuration(oneShotMaxDuration);
                float playFor = (forced > 0f) ? Mathf.Min(forced, naturalDur) : naturalDur;

                wateringAudio.clip = oneShotWaterClip;
                wateringAudio.Play();

                if (playLimiterCo != null) StopCoroutine(playLimiterCo);
                playLimiterCo = StartCoroutine(StopAfter(playFor));
            }
        }
        else
        {
            if (playLimiterCo != null) { StopCoroutine(playLimiterCo); playLimiterCo = null; }
            if (wateringAudio.isPlaying) wateringAudio.Stop();
            wateringAudio.loop = false;
            wateringAudio.clip = null;
        }
    }

    /// <summary> Rövid, fix idejű locsolás (pl. kattintásra). </summary>
    public void PlayWaterFor(float seconds)
    {
        seconds = Mathf.Max(0.05f, seconds);
        StopAllCoroutines();
        playLimiterCo = null;
        StartCoroutine(CoPlayWaterFor(seconds));
    }

    IEnumerator CoPlayWaterFor(float seconds)
    {
        SetWatering(true);
        // ha loop klip van, a SetWatering(true) indítja – mi csak időzítünk
        yield return new WaitForSeconds(seconds);
        SetWatering(false);
    }

    /// <summary>
    /// Ha syncAudioToAnim igaz, megpróbáljuk az animáció idejét használni;
    /// egyébként a paramétert (maxDuration) vesszük figyelembe.
    /// 0 vagy kisebb érték: nincs limit.
    /// </summary>
    float CalcDesiredDuration(float maxDuration)
    {
        float animDur = 0f;
        if (syncAudioToAnim)
        {
            AnimationClip clip = wateringAnimClip ? wateringAnimClip : FindClipByBoolParam(isWateringParam);
            if (clip != null)
            {
                float animSpeed = animator ? Mathf.Max(0.01f, animator.speed) : 1f;
                animDur = clip.length / animSpeed;
            }
        }

        float limit = 0f;
        if (animDur > 0f && maxDuration > 0f) limit = Mathf.Min(animDur, maxDuration);
        else if (animDur > 0f) limit = animDur;
        else if (maxDuration > 0f) limit = maxDuration;

        return Mathf.Max(0f, limit);
    }

    AnimationClip FindClipByBoolParam(string boolParam)
    {
        // egyszerű heuristic: keressünk egy klipet, aminek a neve tartalmazza a param nevét
        if (!animator || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(boolParam)) return null;
        var clips = animator.runtimeAnimatorController.animationClips;
        if (clips == null || clips.Length == 0) return null;

        // prior: név tartalmazza a param nevét
        for (int i = 0; i < clips.Length; i++)
            if (clips[i] && clips[i].name.ToLower().Contains(boolParam.ToLower()))
                return clips[i];

        // fallback: az első nem-null klip
        return clips[0];
    }

    IEnumerator StopAfter(float seconds)
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, seconds));
        if (wateringAudio && wateringAudio.isPlaying)
            wateringAudio.Stop();
        playLimiterCo = null;
    }
}
