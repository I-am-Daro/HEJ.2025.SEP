using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsMenuUI : MonoBehaviour
{
    [Header("Mixer (azzal az assettel, amit a boot is használ)")]
    [SerializeField] AudioMixer mixer;
    [SerializeField] string masterParam = "MasterVol";
    [SerializeField] string musicParam = "MusicVol";
    [SerializeField] string sfxParam = "SFXVol";
    [SerializeField, Range(-80f, 0f)] float minDb = -80f;

    [Header("Sliders (0..1)")]
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;

    void OnEnable()
    {
        // A csúszkák mindig a STORE-ból töltődnek
        var st = VolumeState.I;
        if (st != null)
        {
            if (masterSlider) masterSlider.value = st.master;
            if (musicSlider) musicSlider.value = st.music;
            if (sfxSlider) sfxSlider.value = st.sfx;
        }

        // Listenerek
        if (masterSlider) masterSlider.onValueChanged.AddListener(v => OnChangedMaster(v));
        if (musicSlider) musicSlider.onValueChanged.AddListener(v => OnChangedMusic(v));
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(v => OnChangedSfx(v));
    }

    void OnDisable()
    {
        if (masterSlider) masterSlider.onValueChanged.RemoveAllListeners();
        if (musicSlider) musicSlider.onValueChanged.RemoveAllListeners();
        if (sfxSlider) sfxSlider.onValueChanged.RemoveAllListeners();
    }

    void OnChangedMaster(float v)
    {
        var st = VolumeState.I; if (st == null) return;
        st.master = v; st.SaveToPrefs();
        st.ApplyToMixer(mixer, masterParam, musicParam, sfxParam, minDb);
    }

    void OnChangedMusic(float v)
    {
        var st = VolumeState.I; if (st == null) return;
        st.music = v; st.SaveToPrefs();
        st.ApplyToMixer(mixer, masterParam, musicParam, sfxParam, minDb);
    }

    void OnChangedSfx(float v)
    {
        var st = VolumeState.I; if (st == null) return;
        st.sfx = v; st.SaveToPrefs();
        st.ApplyToMixer(mixer, masterParam, musicParam, sfxParam, minDb);
    }
}
