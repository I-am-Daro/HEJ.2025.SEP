using System.Collections;
using UnityEngine;

/// A locsolókanna kézbevételét kezeli:
/// - világméret fix (nem nő össze a Player skálájával)
/// - Player flipX-hez igazítás (irány + offset tükrözés)
/// - öntözés animáció + hang
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
    [Tooltip("A kanna saját Animator-a (pl. kis vízsugár anim). Opcionális.")]
    public Animator animator;
    [Tooltip("Bool param neve, ami jelzi, hogy épp öntözünk-e.")]
    public string isWateringParam = "IsWatering";

    [Header("Audio")]
    [Tooltip("A kanna saját AudioSource-a (ha nincs, létrejön).")]
    public AudioSource wateringAudio;
    [Tooltip("Loopoló csobogás, amíg öntözünk.")]
    public AudioClip loopWaterClip;
    [Tooltip("Egyszeri spricc hang rövid öntözésnél.")]
    public AudioClip oneShotWaterClip;
    [Range(0f, 1f)] public float waterVolume = 0.9f;
    [Range(0.5f, 1.5f)] public float waterPitch = 1.0f;

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
    Vector3 originalLocalScale; // EREDETI világméret referenciához

    // „Home” – általában a Station CanAnchor-ja
    Transform homeParent;
    Vector3 homeLocalPos;
    Quaternion homeLocalRot;
    Vector3 homeLocalScale = Vector3.one;

    // Carrier (player) sprite-je a flipX figyeléséhez
    SpriteRenderer carrierSprite;
    bool lastCarrierFlipX;

    // világméret-tartás: ezt a világ skálát szeretnénk megtartani kézben is
    Vector3 desiredWorldScale;

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

        // induló világméret, amit szeretnénk megtartani kézben is
        desiredWorldScale = transform.lossyScale;

        // default home: születéskori hely
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
            wateringAudio.volume = waterVolume;
            wateringAudio.pitch = waterPitch;
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
        go.transform.localScale = Vector3.one; // fontos: unit scale
        return go.transform;
    }

    /// <summary> Kézbe vétel. </summary>
    public void PickUp(PlayerStats player, PlayerInventory inv)
    {
        if (IsCarried || player == null || inv == null) return;
        if (!inv.TryTakeCan(this)) return; // ha már nála van vmi, false

        CarrierAnchor = FindOrMakeHandAnchor(player.transform, handAnchorName);
        CarrierAnchor.localScale = Vector3.one; // ne torzítson a kéz-ankor

        // Player sprite a flip figyeléséhez
        carrierSprite = player.GetComponentInChildren<SpriteRenderer>(true);
        lastCarrierFlipX = carrierSprite ? carrierSprite.flipX : false;

        // parentelés
        transform.SetParent(CarrierAnchor, false);
        transform.localPosition = carryLocalOffset;
        transform.localRotation = Quaternion.identity;

        // → világméret fix: localScale = desiredWorldScale / parent.lossyScale
        ApplyCompensatedScale(flipX: lastCarrierFlipX);

        if (col) col.enabled = false;

        if (srs != null)
            for (int i = 0; i < srs.Length; i++)
                srs[i].sortingOrder = originalOrders[i] + carrySortingOrderBoost;

        IsCarried = true;
    }

    /// <summary> Visszarakás a “home”-ra. </summary>
    public void PutBackHome()
    {
        StopAllCoroutines();
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

        // csak akkor dolgozzunk, ha változott a flip, vagy ritkán (védő jelleggel)
        if (flip != lastCarrierFlipX)
        {
            // offset tükrözése
            if (mirrorOffsetOnFlip)
            {
                Vector3 off = carryLocalOffset;
                off.x = Mathf.Abs(off.x) * (flip ? -1f : 1f);
                transform.localPosition = off;
            }

            // skála frissítése (flip-pel)
            ApplyCompensatedScale(flip);

            lastCarrierFlipX = flip;
        }
    }

    // Parent lossyScale ellensúlyozása + opcionális flip X irányban
    void ApplyCompensatedScale(bool flipX)
    {
        if (CarrierAnchor == null) return;

        Vector3 p = CarrierAnchor.lossyScale;
        // védő: ne osztjunk nullával
        float cx = Mathf.Approximately(p.x, 0f) ? 1f : p.x;
        float cy = Mathf.Approximately(p.y, 0f) ? 1f : p.y;

        float sx = desiredWorldScale.x / cx;
        float sy = desiredWorldScale.y / cy;

        // flipX → negatív X skála (sprite tükör)
        transform.localScale = new Vector3(flipX ? -Mathf.Abs(sx) : Mathf.Abs(sx), Mathf.Abs(sy), 1f);
    }

    // ================= ANIM + AUDIO API =================

    /// <summary> Folyamatos öntözés on/off (pl. gomb tartás alatt). </summary>
    public void SetWatering(bool on)
    {
        if (animator && !string.IsNullOrEmpty(isWateringParam))
            animator.SetBool(isWateringParam, on);

        if (wateringAudio)
        {
            if (on)
            {
                if (loopWaterClip)
                {
                    wateringAudio.loop = true;
                    wateringAudio.clip = loopWaterClip;
                    if (!wateringAudio.isPlaying) wateringAudio.Play();
                }
                else if (oneShotWaterClip)
                {
                    wateringAudio.loop = false;
                    wateringAudio.PlayOneShot(oneShotWaterClip, waterVolume);
                }
            }
            else
            {
                if (wateringAudio.isPlaying) wateringAudio.Stop();
                wateringAudio.loop = false;
                wateringAudio.clip = null;
            }
        }
    }

    /// <summary> Rövid, időzített öntözés (pl. egy gombnyomásra). </summary>
    public void PulseWatering(float seconds = 0.6f)
    {
        StopAllCoroutines();
        StartCoroutine(CoPulse(seconds));
    }

    IEnumerator CoPulse(float seconds)
    {
        SetWatering(true);
        yield return new WaitForSeconds(Mathf.Max(0.05f, seconds));
        SetWatering(false);
    }
}
