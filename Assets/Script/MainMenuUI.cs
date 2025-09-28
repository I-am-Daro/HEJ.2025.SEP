using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] Button continueBtn;
    [SerializeField] Button newGameBtn;
    [SerializeField] Button optionsBtn;
    [SerializeField] Button quitBtn;

    [Header("Panels")]
    [SerializeField] GameObject optionsPanel; // <- EZ HIÁNYZOTT

    [Header("Flow")]
    [SerializeField] string firstPlayableScene = "Ship_Interior";

    void Awake()
    {
        CleanupStrayPlayerInMenu();
        // Continue
        if (continueBtn)
        {
            bool hasSave = SaveSystem.HasSave();
            continueBtn.interactable = hasSave;
            continueBtn.onClick.AddListener(OnContinue);
        }

        if (newGameBtn) newGameBtn.onClick.AddListener(OnNewGame);
        if (optionsBtn) optionsBtn.onClick.AddListener(OpenOptions);
        if (quitBtn) quitBtn.onClick.AddListener(QuitGame);

        if (optionsPanel) optionsPanel.SetActive(false);
    }

    void CleanupStrayPlayerInMenu()
    {
        Time.timeScale = 1f;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;

        // Emlékezzünk rá, hogy ezt kapcsoltuk ki (Continue-kor ebből élesztünk)
        SaveSystem.CacheDeactivatedPlayer(player);

        // Fagyaszd a fizikát, állítsd 0-ra a sebességet
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb)
        {
#if UNITY_2022_3_OR_NEWER
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
#else
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
#endif
            rb.simulated = false;
        }

        // Ne reagáljon inputra a menüben
        var plat = player.GetComponent<PlatformerController2D>(); if (plat) plat.enabled = false;
        var top = player.GetComponent<TopDownMover>(); if (top) top.enabled = false;
        var svc = player.GetComponent<PlayerMovementService>(); if (svc) svc.enabled = false;

        // Kikapcsoljuk – de megmarad a DontDestroyOnLoad alatt
        player.SetActive(false);
    }

    void OnNewGame()
    {
        SaveSystem.DeleteSave();                          // <-- induljon tiszta lappal
        Time.timeScale = 1f;
        SceneManager.LoadScene(firstPlayableScene, LoadSceneMode.Single);
    }

    void OnContinue()
    {
        Time.timeScale = 1f;

        // Ha van mentés: a SaveSystem intézze a betöltést + pályaváltást + elhelyezést.
        if (SaveSystem.HasSave())
        {
            SaveSystem.LoadCheckpointAndPlacePlayer();    // <-- ugyanazt hívd, mint halálnál
            return;
        }

        // Fallback: ha még sincs mentés, kezdjünk új játékot
        SceneManager.LoadScene(firstPlayableScene, LoadSceneMode.Single);
    }

    void OpenOptions()
    {
        if (optionsPanel) optionsPanel.SetActive(true);
    }

    // Ezt kösd az Options panel "Close" gombjára (OnClick)
    public void CloseOptions()
    {
        if (optionsPanel) optionsPanel.SetActive(false);
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
