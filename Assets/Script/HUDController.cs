using UnityEngine;
using UnityEngine.SceneManagement;

public class HUDController : MonoBehaviour
{
    [SerializeField] UIStatusBar o2Bar, energyBar, hungerBar;
    [SerializeField] PlayerStats stats;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryBind();
    }

    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene s, LoadSceneMode m) => TryBind();

    void TryBind()
    {
        if (!stats) stats = FindObjectOfType<PlayerStats>();
    }

    void Update()
    {
        if (!stats) return;
        o2Bar.SetValue(stats.oxygen, true);
        energyBar.SetValue(stats.energy, true);
        hungerBar.SetValue(stats.hunger, true);
    }
}
