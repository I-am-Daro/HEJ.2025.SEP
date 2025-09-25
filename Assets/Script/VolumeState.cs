using UnityEngine;
using UnityEngine.Audio;

[DefaultExecutionOrder(-10000)]
public class VolumeState : MonoBehaviour
{
    public static VolumeState I { get; private set; }

    // Slider jellegű 0..1 értékek
    [Range(0f, 1f)] public float master = 0.8f;
    [Range(0f, 1f)] public float music = 0.7f;
    [Range(0f, 1f)] public float sfx = 0.8f;

    const string K_Master = "vol_master";
    const string K_Music = "vol_music";
    const string K_Sfx = "vol_sfx";

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        LoadFromPrefs();
    }

    public void LoadFromPrefs()
    {
        master = PlayerPrefs.GetFloat(K_Master, master);
        music = PlayerPrefs.GetFloat(K_Music, music);
        sfx = PlayerPrefs.GetFloat(K_Sfx, sfx);
    }

    public void SaveToPrefs()
    {
        PlayerPrefs.SetFloat(K_Master, master);
        PlayerPrefs.SetFloat(K_Music, music);
        PlayerPrefs.SetFloat(K_Sfx, sfx);
        PlayerPrefs.Save();
    }

    // Mixerre alkalmazás (bármikor hívható)
    public void ApplyToMixer(AudioMixer mixer, string masterParam, string musicParam, string sfxParam, float minDb = -80f)
    {
        if (!mixer) return;
        Set(mixer, masterParam, master, minDb);
        Set(mixer, musicParam, music, minDb);
        Set(mixer, sfxParam, sfx, minDb);
    }

    static void Set(AudioMixer m, string param, float v01, float minDb)
    {
        if (string.IsNullOrEmpty(param)) return;
        // 0 → minDb (némítás), 1 → 0 dB (lineáris→log konverzió)
        float dB = (v01 <= 0.0001f) ? minDb : Mathf.Log10(Mathf.Clamp01(v01)) * 20f;
        if (dB < minDb) dB = minDb;
        m.SetFloat(param, dB);
    }
}
