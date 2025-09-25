using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string masterParam = "MasterVol";
    [SerializeField] private string musicParam = "MusicVol";
    [SerializeField] private string sfxParam = "SFXVol";
    [SerializeField, Range(-80f, 0f)] private float minDb = -80f;

    [Header("Sliders (opcionális)")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    // PlayerPrefs kulcsok – ugyanazok mindenhol
    private const string K_Master = "vol_master";
    private const string K_Music = "vol_music";
    private const string K_Sfx = "vol_sfx";

    void Start()
    {
        // MASTER
        InitTrack(masterSlider, K_Master, masterParam, 0.8f);
        // MUSIC
        InitTrack(musicSlider, K_Music, musicParam, 0.7f);
        // SFX
        InitTrack(sfxSlider, K_Sfx, sfxParam, 0.8f);
    }

    /// Egy sáv inicializálása a "képes" logikával:
    /// ha van mentett érték -> betölt + alkalmaz; különben a jelenlegi slider értéket
    /// (vagy defaultot) mentjük és alkalmazzuk.
    private void InitTrack(Slider slider, string key, string mixerParam, float @default)
    {
        float v01;

        if (PlayerPrefs.HasKey(key))
        {
            // Load
            v01 = PlayerPrefs.GetFloat(key);
            if (slider) slider.value = v01;   // slider szinkron
            SetMixer(mixerParam, v01);        // AZONNAL a mixerre
        }
        else
        {
            // Nincs mentés: a slider aktuális állása vagy a default
            v01 = slider ? slider.value : @default;
            v01 = Mathf.Clamp01(v01);
            SetMixer(mixerParam, v01);
            PlayerPrefs.SetFloat(key, v01);
        }

        // Ha rákötöd az OnValueChanged-re, azonnal ment és alkalmaz
        if (slider)
            slider.onValueChanged.AddListener(val =>
            {
                val = Mathf.Clamp01(val);
                SetMixer(mixerParam, val);
                PlayerPrefs.SetFloat(key, val);
            });
    }

    /// 0..1 → dB és beírja a Mixerbe
    private void SetMixer(string param, float slider01)
    {
        if (mixer == null || string.IsNullOrEmpty(param)) return;

        // 0 teljes némítás (minDb), 1 = 0 dB; közte logaritmikus görbe
        float dB = (slider01 <= 0.0001f)
            ? minDb
            : Mathf.Log10(Mathf.Clamp(slider01, 0.0001f, 1f)) * 20f;

        if (dB < minDb) dB = minDb;
        mixer.SetFloat(param, dB);
    }

    // Ezeket a metódusokat akkor használd, ha NINCS slidered, de
    // gombbal/egyéb UI-ból szeretnél állítani:
    public void SetMaster01(float v) { v = Mathf.Clamp01(v); SetMixer(masterParam, v); PlayerPrefs.SetFloat(K_Master, v); }
    public void SetMusic01(float v) { v = Mathf.Clamp01(v); SetMixer(musicParam, v); PlayerPrefs.SetFloat(K_Music, v); }
    public void SetSfx01(float v) { v = Mathf.Clamp01(v); SetMixer(sfxParam, v); PlayerPrefs.SetFloat(K_Sfx, v); }
}
