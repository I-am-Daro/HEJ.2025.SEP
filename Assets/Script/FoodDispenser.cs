using System.Collections;
using UnityEngine;
using UnityEngine.Audio;   // <<< ÚJ

[RequireComponent(typeof(Collider2D))]
public class FoodDispenser : MonoBehaviour, IInteractable
{
    [Header("Use Settings")]
    public int foodUnitsPerUse = 1;
    public float nutritionPerUnit = 25f;

    [Header("Animation")]
    [SerializeField] Animator animator;
    [SerializeField] string doorBoolParam = "IsOpen";
    public float openDuration = 0.4f;
    public float eatDuration = 0.8f;
    public float closeDuration = 0.4f;

    [Header("Audio (optional)")]
    public AudioSource sfxSource;
    public AudioClip openSfx;
    public AudioClip eatSfx;
    public AudioClip closeSfx;

    [Header("Audio Routing")]
    [Tooltip("Kösd ide a Mixer SFX csoportját, hogy az Options SFX csúszka ezt is szabályozza.")]
    [SerializeField] AudioMixerGroup sfxBus;   // <<< ÚJ

    [Header("Control")]
    public bool lockMovementDuringUse = true;

    [Tooltip("Ha igaz, a használat végén csak a PlatformerController2D-t engedélyezzük vissza (belsõ tér).")]
    public bool restoreOnlyPlatformer = true;

    bool inUse;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void Awake()
    {
        if (!sfxSource)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f; // 2D SFX
        }
        ApplyMixerGroup();  // <<< ÚJ
    }

    void OnValidate()
    {
        // Inspectorban késõbb beállított Sfx Bus azonnal érvényesüljön
        ApplyMixerGroup();  // <<< ÚJ
    }

    void ApplyMixerGroup()
    {
        if (sfxSource && sfxBus)
            sfxSource.outputAudioMixerGroup = sfxBus;
    }

    public string GetPrompt() => inUse ? "" : "Eat (E)";

    public void Interact(PlayerStats player)
    {
        if (inUse || !player) return;

        var inv = player.GetComponent<PlayerInventory>();
        if (!inv)
        {
            Debug.LogWarning("[FoodDispenser] PlayerInventory missing.");
            return;
        }
        if (inv.foodUnits < foodUnitsPerUse)
        {
            Debug.Log("[FoodDispenser] No food.");
            return;
        }

        StartCoroutine(UseSequence(player, inv));
    }

    IEnumerator UseSequence(PlayerStats player, PlayerInventory inv)
    {
        inUse = true;

        // 1) Mozgás lock
        MovementLock mlock = default;
        if (lockMovementDuringUse) mlock = MovementLock.Apply(player.gameObject);

        // 2) Ajtó nyit
        SetDoor(true);
        Play(openSfx);
        if (openDuration > 0f) yield return new WaitForSeconds(openDuration);

        // 3) Evés
        inv.foodUnits -= foodUnitsPerUse;
        inv.RaiseChanged();
        player.Eat(Mathf.Max(0f, nutritionPerUnit) * foodUnitsPerUse);
        Play(eatSfx);
        if (eatDuration > 0f) yield return new WaitForSeconds(eatDuration);

        // 4) Ajtó zár
        SetDoor(false);
        Play(closeSfx);
        if (closeDuration > 0f) yield return new WaitForSeconds(closeDuration);

        // 5) Mozgás unlock
        if (lockMovementDuringUse) mlock.Release(restoreOnlyPlatformer);

        inUse = false;
    }

    void SetDoor(bool open)
    {
        if (!animator || string.IsNullOrEmpty(doorBoolParam)) return;
        animator.SetBool(doorBoolParam, open);
    }

    void Play(AudioClip clip)
    {
        if (!clip) return;
        if (sfxSource) sfxSource.PlayOneShot(clip);
        else AudioSource.PlayClipAtPoint(clip, transform.position);
    }

    // ---------- Movement lock helper ----------
    struct MovementLock
    {
        readonly Behaviour plat2D; readonly bool plat2DWasOn;
        readonly Behaviour topDown; readonly bool topDownWasOn;
        readonly Behaviour moveService; readonly bool moveServiceWasOn;
        readonly Rigidbody2D rb; readonly Vector2 prevVel; readonly float prevAng;

        MovementLock(Behaviour p2d, bool p2dOn,
                     Behaviour td, bool tdOn,
                     Behaviour svc, bool svcOn,
                     Rigidbody2D r, Vector2 pv, float pa)
        {
            plat2D = p2d; plat2DWasOn = p2dOn;
            topDown = td; topDownWasOn = tdOn;
            moveService = svc; moveServiceWasOn = svcOn;
            rb = r; prevVel = pv; prevAng = pa;
        }

        public static MovementLock Apply(GameObject player)
        {
            var p2d = player.GetComponent<PlatformerController2D>();
            var td = player.GetComponent<TopDownMover>();
            var svc = player.GetComponent<PlayerMovementService>();
            var r = player.GetComponent<Rigidbody2D>();

            bool p2dOn = p2d && p2d.enabled;
            bool tdOn = td && td.enabled;
            bool svcOn = svc && svc.enabled;

            // állítsuk meg
            Vector2 pv = Vector2.zero;
            float pa = 0f;
            if (r)
            {
#if UNITY_2022_3_OR_NEWER
                pv = r.linearVelocity; pa = r.angularVelocity;
                r.linearVelocity = Vector2.zero;
                r.angularVelocity = 0f;
#else
                pv = r.velocity; pa = r.angularVelocity;
                r.velocity = Vector2.zero;
                r.angularVelocity = 0f;
#endif
            }

            if (p2d) p2d.enabled = false;
            if (td) td.enabled = false;
            if (svc) svc.enabled = false;

            return new MovementLock(p2d, p2dOn, td, tdOn, svc, svcOn, r, pv, pa);
        }

        public void Release(bool onlyPlatformer)
        {
            if (onlyPlatformer)
            {
                if (plat2D) plat2D.enabled = plat2DWasOn;   // csak platformer
                if (topDown) topDown.enabled = false;
                if (moveService) moveService.enabled = false;
            }
            else
            {
                if (plat2D) plat2D.enabled = plat2DWasOn;
                if (topDown) topDown.enabled = topDownWasOn;
                if (moveService) moveService.enabled = moveServiceWasOn;
            }

            if (rb)
            {
#if UNITY_2022_3_OR_NEWER
                rb.linearVelocity = prevVel;
                rb.angularVelocity = prevAng;
#else
                rb.velocity = prevVel;
                rb.angularVelocity = prevAng;
#endif
            }
        }
    }
}
