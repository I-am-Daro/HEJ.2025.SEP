using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-9999)]
public class AudioVolumeBoot : MonoBehaviour
{
    [Header("Mixer + exposed paramok")]
    [SerializeField] AudioMixer mixer;
    [SerializeField] string masterParam = "MasterVol";
    [SerializeField] string musicParam = "MusicVol";
    [SerializeField] string sfxParam = "SFXVol";
    [SerializeField, Range(-80f, 0f)] float minDb = -80f;

    void OnEnable()
    {
        // Első frame-ben AZONNAL alkalmazzuk
        ApplyNow();
        // Utána minden scene-betöltésnél is
        SceneManager.sceneLoaded += OnSceneLoaded;
        AudioSettings.OnAudioConfigurationChanged += _ => ApplyNow();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        AudioSettings.OnAudioConfigurationChanged -= _ => ApplyNow();
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m) => ApplyNow();

    void ApplyNow()
    {
        // Keresünk VolumeState-et (DDOL-ból); ha nincs, ideiglenesen betöltünk Prefsből
        var store = VolumeState.I;
        if (store != null)
        {
            store.ApplyToMixer(mixer, masterParam, musicParam, sfxParam, minDb);
        }
        else
        {
            float master = PlayerPrefs.GetFloat("vol_master", 0.8f);
            float music = PlayerPrefs.GetFloat("vol_music", 0.7f);
            float sfx = PlayerPrefs.GetFloat("vol_sfx", 0.8f);
            // ugyanaz a konverzió, mint a store-ban
            Set(mixer, masterParam, master, minDb);
            Set(mixer, musicParam, music, minDb);
            Set(mixer, sfxParam, sfx, minDb);
        }
    }

    static void Set(AudioMixer m, string param, float v01, float minDb)
    {
        if (!m || string.IsNullOrEmpty(param)) return;
        float dB = (v01 <= 0.0001f) ? minDb : Mathf.Log10(Mathf.Clamp01(v01)) * 20f;
        if (dB < minDb) dB = minDb;
        m.SetFloat(param, dB);
    }
}
