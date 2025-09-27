using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class SleepPod : MonoBehaviour, IInteractable
{
    [Header("Sleep gating")]
    [Tooltip("Csak akkor enged aludni, ha az ENERGY ez alá esik.")]
    [SerializeField, Range(0f, 100f)] float energySleepThresholdPercent = 50f;

    [Header("Sleep Effects")]
    [Tooltip("Hány %-ot nőjön a Hunger alváskor.")]
    [SerializeField] float hungerIncreaseOnSleep = 10f;

    [Tooltip("Hány darab O2 item fogyjon el alvás alatt az inventoryból.")]
    [SerializeField] int oxygenItemsConsumedOnSleep = 1;

    [Header("Screen fade (optional)")]
    [SerializeField] CanvasGroup fadeCanvas;     // teljes képernyős fekete CanvasGroup (alpha 0..1)
    [SerializeField] float fadeInDuration = 0.35f;
    [SerializeField] float blackHoldDuration = 0.80f;
    [SerializeField] float fadeOutDuration = 0.35f;
    [SerializeField] string fadeCanvasTag = "SleepFade";       // tedd ezt a Tag-et a fade panel GO-ra (opcionális)
    [SerializeField] string fadeCanvasName = "SleepFadeCanvas"; // vagy adj neki ezt a nevet (opcionális)

    [Header("Day label on black (optional)")]
    [Tooltip("TMP felirat a nap kiírásához (előnyben részesített).")]
    [SerializeField] TextMeshProUGUI dayLabelTMP;
    [Tooltip("Ha nincs TMP, használható sima UI Text is.")]
    [SerializeField] Text dayLabelUI;
    [Tooltip("Felirat előtagja (pl. \"Day \", \"Nap \").")]
    [SerializeField] string dayPrefix = "Day ";
    [Tooltip("Megjelenítse-e a napot a fekete képernyő alatt?")]
    [SerializeField] bool showDayOnFade = true;

    [Header("Control lock")]
    [SerializeField] bool lockMovementDuringSleep = true;
    [Tooltip("Belső térben: csak a platformer kontrollert engedjük vissza.")]
    [SerializeField] bool restoreOnlyPlatformer = true;

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    public string GetPrompt() => "Sleep";

    public void Interact(PlayerStats player)
    {
        if (!player) return;

        // csak ha elég fáradt
        if (player.energy >= energySleepThresholdPercent)
        {
            Debug.Log("[SleepPod] Not tired enough to sleep yet.");
            return;
        }

        StartCoroutine(SleepSequence(player));
    }

   

    void OnEnable() { RebindFadeCanvas(); }

    void RebindFadeCanvas()
    {
        if (fadeCanvas && fadeCanvas.gameObject) return;

        CanvasGroup cg = null;

        if (!string.IsNullOrEmpty(fadeCanvasTag))
        {
            var go = GameObject.FindGameObjectWithTag(fadeCanvasTag);
            if (go) cg = go.GetComponentInChildren<CanvasGroup>(true);
        }
        if (!cg && !string.IsNullOrEmpty(fadeCanvasName))
        {
            var go = GameObject.Find(fadeCanvasName);
            if (go) cg = go.GetComponentInChildren<CanvasGroup>(true);
        }
        if (!cg)
        {
            // végső fallback: első elérhető CanvasGroup (InActive-ben is keres)
#if UNITY_2022_3_OR_NEWER
        cg = FindFirstObjectByType<CanvasGroup>(FindObjectsInactive.Include);
#else
            cg = FindObjectOfType<CanvasGroup>(true);
#endif
        }

        fadeCanvas = cg;

        // új panel → a felirat referenciákat kerestessük újra
        dayLabelTMP = null;
        dayLabelUI = null;

        // tegyük ismert állapotba
        if (fadeCanvas)
        {
            fadeCanvas.gameObject.SetActive(true);
            fadeCanvas.alpha = 0f;
            fadeCanvas.blocksRaycasts = false;
        }
    }

    IEnumerator SleepSequence(PlayerStats player)
    {
        RebindFadeCanvas();          // << ÚJ
        EnsureDayLabelRefs();        // (ez most már az új panel alatt keres)

        MovementLock mlock = default;
        if (lockMovementDuringSleep) mlock = MovementLock.Apply(player.gameObject);

        // induláskor biztosan elérhető és látható-e a panel GO
        if (fadeCanvas)
        {
            fadeCanvas.gameObject.SetActive(true);
            fadeCanvas.blocksRaycasts = true;   // blokkolja az inputot a fade alatt
        }

        // fade in
        yield return FadeTo(1f, fadeInDuration);

        ApplySleepEffects(player);

        if (showDayOnFade)
        {
            int day = DayNightSystem.Instance ? DayNightSystem.Instance.CurrentDay : 1;
            SetDayLabelVisible(true, $"{dayPrefix}{day}");
        }

        if (blackHoldDuration > 0f) yield return new WaitForSeconds(blackHoldDuration);

        if (showDayOnFade) SetDayLabelVisible(false, null);
        yield return FadeTo(0f, fadeOutDuration);

        if (fadeCanvas) fadeCanvas.blocksRaycasts = false;

        if (lockMovementDuringSleep) mlock.Release(restoreOnlyPlatformer);

        Debug.Log("[SleepPod] Slept: effects applied, new day started.");
    }

    void ApplySleepEffects(PlayerStats player)
    {
        // ENERGY feltöltés
        player.FullRest();

        // HUNGER növelés
        player.hunger = Mathf.Clamp(player.hunger + Mathf.Max(0f, hungerIncreaseOnSleep), 0f, 100f);

        // O2 item fogyasztás
        var inv = player.GetComponent<PlayerInventory>();
        if (inv && oxygenItemsConsumedOnSleep > 0 && inv.oxygenUnits > 0)
        {
            int take = Mathf.Min(oxygenItemsConsumedOnSleep, inv.oxygenUnits);
            inv.oxygenUnits -= take;
            inv.RaiseChanged();
        }

        // Nap előrelépés (növények, respawnok, stb.)
        DayNightSystem.Instance?.AdvanceDay();

        // Gyors mentés
        var save = new GameSave
        {
            day = DayNightSystem.Instance ? DayNightSystem.Instance.CurrentDay : 1,
            sceneName = SceneManager.GetActiveScene().name,
            playerPosition = player.transform.position,
            oxygen = player.oxygen,
            energy = player.energy,
            hunger = player.hunger
        };
        SaveSystem.Save(save);
    }

    IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (!fadeCanvas)
        {
            RebindFadeCanvas();
            if (!fadeCanvas) { yield return null; yield break; }
        }

        if (duration <= 0f)
        {
            fadeCanvas.alpha = targetAlpha;
            yield return null;
            yield break;
        }

        float start = fadeCanvas.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            fadeCanvas.alpha = Mathf.Lerp(start, targetAlpha, k);
            yield return null;
        }
        fadeCanvas.alpha = targetAlpha;
    }


    // -------- Day label helpers --------
    void EnsureDayLabelRefs()
    {
        if (!fadeCanvas) return;

        // ha nincs beállítva, próbáljuk automatikusan keresni
        if (!dayLabelTMP)
            dayLabelTMP = fadeCanvas.GetComponentInChildren<TextMeshProUGUI>(true);
        if (!dayLabelTMP && !dayLabelUI)
            dayLabelUI = fadeCanvas.GetComponentInChildren<Text>(true);

        // induláskor rejtsük el
        SetDayLabelVisible(false, null);
    }

    void SetDayLabelVisible(bool visible, string text)
    {
        if (dayLabelTMP)
        {
            if (text != null) dayLabelTMP.text = text;
            dayLabelTMP.gameObject.SetActive(visible);
        }
        if (dayLabelUI)
        {
            if (text != null) dayLabelUI.text = text;
            dayLabelUI.gameObject.SetActive(visible);
        }
    }

    // ---------- Movement lock helper (ugyanaz a minta, mint korábban) ----------
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
                if (plat2D) plat2D.enabled = plat2DWasOn;
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
