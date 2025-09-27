using UnityEngine;
using UnityEngine.UI;

public class OptionsMenuUI : MonoBehaviour
{
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;

    VolumeManager VM =>
        VolumeManager.Instance != null ? VolumeManager.Instance :
        (VolumeManager.I != null ? VolumeManager.I :
         FindFirstObjectByType<VolumeManager>(FindObjectsInactive.Include));

    void OnEnable()
    {
        if (!VM) return;

        // értékek betöltése csúszkákra anélkül, hogy eventet lőnénk
        if (masterSlider) masterSlider.SetValueWithoutNotify(VM.Master);
        if (musicSlider) musicSlider.SetValueWithoutNotify(VM.Music);
        if (sfxSlider) sfxSlider.SetValueWithoutNotify(VM.Sfx);

        // feliratkozások
        if (masterSlider) masterSlider.onValueChanged.AddListener(v => VM.SetMaster(v));
        if (musicSlider) musicSlider.onValueChanged.AddListener(v => VM.SetMusic(v));
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(v => VM.SetSfx(v));
    }

    void OnDisable()
    {
        if (masterSlider) masterSlider.onValueChanged.RemoveAllListeners();
        if (musicSlider) musicSlider.onValueChanged.RemoveAllListeners();
        if (sfxSlider) sfxSlider.onValueChanged.RemoveAllListeners();
    }
}
