using System.Collections;
using UnityEngine;

public class ElevatorDoor : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] string boolParam = "IsOpen";

    // Animator state nevek (az állapotok „végi” képkockájára ugrunk SnapTo-kor)
    [SerializeField] string closedStateName = "Closed";
    [SerializeField] string openStateName = "Open";

    [SerializeField] SpriteRenderer frontOverlay;

    // ---------- SFX ----------
    [Header("SFX")]
    [SerializeField] AudioSource audioSource;    // ha üres, Awake-ben létrejön
    [SerializeField] AudioClip openClip;
    [SerializeField] AudioClip closeClip;

    [Header("Volumes & Pitch")]
    [Range(0f, 1f)] public float openVolume = 1f;
    [Range(0.2f, 3f)] public float openPitch = 1f;
    [Range(0f, 1f)] public float closeVolume = 1f;
    [Range(0.2f, 3f)] public float closePitch = 1f;

    [Header("Max Play Times (sec)")]
    [Tooltip("0 = nincs limit, >0 = max ennyi ideig szól a hang")]
    public float openMaxDuration = 0f;
    public float closeMaxDuration = 0f;

    [Header("Sync SFX to Animation")]
    [Tooltip("Ha be van kapcsolva, a hang a megadott animáció hosszához igazodik.")]
    public bool syncSfxToAnimation = true;

    [Tooltip("Ha itt megadod a nyitó animáció klipjét, pontosan ezt vesszük alapul a hosszhoz.")]
    public AnimationClip openAnimClip;

    [Tooltip("Ha itt megadod a záró animáció klipjét, pontosan ezt vesszük alapul a hosszhoz.")]
    public AnimationClip closeAnimClip;

    Coroutine playingCo;

    void Reset()
    {
        if (!animator) animator = GetComponent<Animator>();
    }

    void Awake()
    {
        if (!audioSource)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f; // 2D
        }
    }

    // --- instant állapot ---
    public void SnapTo(bool open)
    {
        if (animator)
        {
            animator.SetBool(boolParam, open);
            animator.Play(open ? openStateName : closedStateName, 0, 1f);
            animator.Update(0f);
        }
        if (frontOverlay) frontOverlay.enabled = open;
    }

    // duration: opcionális várakozási idő a coroutine végén (nem befolyásolja a hangot)
    public IEnumerator Open(float duration)
    {
        if (animator) animator.SetBool(boolParam, true);
        if (frontOverlay) frontOverlay.enabled = true;

        float sfxDur = ComputeSfxDuration(
            preferClip: openAnimClip,
            fallbackStateOrClipName: openStateName,
            animatorSpeed: animator ? animator.speed : 1f,
            pitch: openPitch,
            maxDuration: openMaxDuration
        );
        PlayClip(openClip, openVolume, openPitch, sfxDur);

        if (duration > 0) yield return new WaitForSeconds(duration);
    }

    public IEnumerator Close(float duration)
    {
        if (animator) animator.SetBool(boolParam, false);

        float sfxDur = ComputeSfxDuration(
            preferClip: closeAnimClip,
            fallbackStateOrClipName: closedStateName,
            animatorSpeed: animator ? animator.speed : 1f,
            pitch: closePitch,
            maxDuration: closeMaxDuration
        );
        PlayClip(closeClip, closeVolume, closePitch, sfxDur);

        if (duration > 0) yield return new WaitForSeconds(duration);
        if (frontOverlay) frontOverlay.enabled = false;
    }

    public void SetOverlay(bool on)
    {
        if (frontOverlay) frontOverlay.enabled = on;
    }

    // ---------- Helpers ----------

    void PlayClip(AudioClip clip, float vol, float pitch, float forcedDuration)
    {
        if (!clip || audioSource == null) return;

        if (playingCo != null) StopCoroutine(playingCo);

        audioSource.clip = clip;
        audioSource.volume = vol;
        audioSource.pitch = pitch;
        audioSource.Play();

        // Ha forcedDuration > 0, akkor pontosan addig szól; különben a (clip/pitch) teljes hosszig.
        float clipTime = clip.length / Mathf.Max(0.01f, pitch);
        float dur = forcedDuration > 0f ? forcedDuration : clipTime;

        playingCo = StartCoroutine(StopAfter(dur));
    }

    IEnumerator StopAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (audioSource.isPlaying) audioSource.Stop();
        playingCo = null;
    }

    float ComputeSfxDuration(AnimationClip preferClip, string fallbackStateOrClipName,
                             float animatorSpeed, float pitch, float maxDuration)
    {
        // Ha nem kérünk szinkront, akkor csak a maxDuration limit érvényes (0 = nincs limit)
        if (!syncSfxToAnimation)
            return Mathf.Max(0f, maxDuration);

        AnimationClip clip = preferClip ? preferClip : FindClipByName(fallbackStateOrClipName);

        if (clip == null)
        {
            // nincs klip → nincs pontos idő, marad csak a maxDuration (0 = nincs limit)
            return Mathf.Max(0f, maxDuration);
        }

        // Valós lejátszási idő = (anim clip hossz / animatorSpeed)
        // A hanghoz pedig még a pitch is számít: ha a hang gyorsabb (pitch>1), rövidebb legyen
        float animTime = clip.length / Mathf.Max(0.01f, animatorSpeed);
        float sfxTime = animTime / Mathf.Max(0.01f, pitch);

        if (maxDuration > 0f) sfxTime = Mathf.Min(sfxTime, maxDuration);
        return Mathf.Max(0.01f, sfxTime);
    }

    AnimationClip FindClipByName(string nameOrState)
    {
        if (!animator || animator.runtimeAnimatorController == null) return null;

        var clips = animator.runtimeAnimatorController.animationClips;
        if (clips == null || clips.Length == 0) return null;

        // 1) pontos név egyezés (legtöbbször a state neve megegyezik a klip nevével)
        for (int i = 0; i < clips.Length; i++)
            if (clips[i] && clips[i].name == nameOrState)
                return clips[i];

        // 2) részleges egyezés (hátha a state neve tartalmazza a klip nevét vagy fordítva)
        for (int i = 0; i < clips.Length; i++)
            if (clips[i] && (clips[i].name.Contains(nameOrState) || nameOrState.Contains(clips[i].name)))
                return clips[i];

        return null;
    }
}
