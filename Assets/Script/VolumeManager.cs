using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
public class VolumeManager : MonoBehaviour
{
    [Header("Mixer + exposed params (same as Options)")]
    [SerializeField] AudioMixer mixer;
    [SerializeField] string masterParam = "MasterVol";
    [SerializeField] string musicParam = "MusicVol";
    [SerializeField] string sfxParam = "SFXVol";
    [SerializeField, Range(-80f, 0f)] float minDb = -80f;

    // PlayerPrefs kulcsok (ugyanazok, mint Optionsban)
    public const string K_Master = "vol_master";
    public const string K_Music = "vol_music";
    public const string K_Sfx = "vol_sfx";

    // Alap csúszka-értékek (0..1)
    [Header("Defaults 0..1")]
    [SerializeField, Range(0f, 1f)] float masterDefault = 0.8f;
    [SerializeField, Range(0f, 1f)] float musicDefault = 0.7f;
    [SerializeField, Range(0f, 1f)] float sfxDefault = 0.8f;

    public static VolumeManager I { get; private set; }
    public static VolumeManager Instance => I;

    public event Action OnVolumeChanged;

    float master01, music01, sfx01;

    // gyors hozzáférés a jelenlegi dB-hez
    public float CurrentMasterDb => ToDb(master01);
    public float CurrentMusicDb => ToDb(music01);
    public float CurrentSfxDb => ToDb(sfx01);
    public float Master => master01;   // vagy ahogy nálad hívják
    public float Music => music01;
    public float Sfx => sfx01;

    // --- régi setter nevek aliasai ---
    public void SetMaster(float v) => SetMaster01(v);
    public void SetMusic(float v) => SetMusic01(v);
    public void SetSfx(float v) => SetSfx01(v);

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // betöltünk 0..1 skálán
        master01 = PlayerPrefs.GetFloat(K_Master, masterDefault);
        music01 = PlayerPrefs.GetFloat(K_Music, musicDefault);
        sfx01 = PlayerPrefs.GetFloat(K_Sfx, sfxDefault);

        ApplyAllToMixer(); // AZONNAL a mixerre tesszük (játék induláskor!)
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        AudioSettings.OnAudioConfigurationChanged += OnAudioCfgChanged;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        AudioSettings.OnAudioConfigurationChanged -= OnAudioCfgChanged;
    }

    void OnSceneLoaded(Scene _, LoadSceneMode __) => ApplyAllToMixer();
    void OnAudioCfgChanged(bool __) => ApplyAllToMixer();

    // --- API (Optionsból hívd ezeket, amikor a slider változik) ---
    public void SetMaster01(float v) { master01 = Mathf.Clamp01(v); Save(K_Master, v); Apply(masterParam, master01); OnVolumeChanged?.Invoke(); }
    public void SetMusic01(float v) { music01 = Mathf.Clamp01(v); Save(K_Music, v); Apply(musicParam, music01); OnVolumeChanged?.Invoke(); }
    public void SetSfx01(float v) { sfx01 = Mathf.Clamp01(v); Save(K_Sfx, v); Apply(sfxParam, sfx01); OnVolumeChanged?.Invoke(); }

    public float GetMaster01() => master01;
    public float GetMusic01() => music01;
    public float GetSfx01() => sfx01;

    // a MenuMusicPlayer ezt hívja
    public float GetCurrentMusicDb() => CurrentMusicDb;

    // --- belsõ segítségek ---
    void ApplyAllToMixer()
    {
        Apply(masterParam, master01);
        Apply(musicParam, music01);
        Apply(sfxParam, sfx01);
        OnVolumeChanged?.Invoke();
    }

    void Apply(string param, float slider01)
    {
        if (!mixer || string.IsNullOrEmpty(param)) return;
        mixer.SetFloat(param, ToDb(slider01));
    }

    float ToDb(float slider01)
    {
        if (slider01 <= 0.0001f) return minDb;                   // némítás
        float dB = Mathf.Log10(Mathf.Clamp(slider01, 0.0001f, 1f)) * 20f;
        return Mathf.Max(dB, minDb);
    }

    static void Save(string key, float v) { PlayerPrefs.SetFloat(key, v); /* PlayerPrefs.Save(); opcionális */ }
}
