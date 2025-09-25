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
        // Continue
        if (continueBtn)
        {
            bool hasSave = PlayerPrefs.HasKey("LastScene");
            continueBtn.interactable = hasSave;
            continueBtn.onClick.AddListener(OnContinue);
        }

        // New Game
        if (newGameBtn) newGameBtn.onClick.AddListener(OnNewGame);

        // Options
        if (optionsBtn) optionsBtn.onClick.AddListener(OpenOptions);

        // Quit
        if (quitBtn) quitBtn.onClick.AddListener(QuitGame);

        // Alapból rejtett Options
        if (optionsPanel) optionsPanel.SetActive(false);
    }

    void OnNewGame()
    {
        PlayerPrefs.DeleteKey("LastScene");
        SceneManager.LoadScene(firstPlayableScene, LoadSceneMode.Single);
    }

    void OnContinue()
    {
        string scene = PlayerPrefs.GetString("LastScene", firstPlayableScene);
        if (string.IsNullOrEmpty(scene)) scene = firstPlayableScene;
        SceneManager.LoadScene(scene, LoadSceneMode.Single);
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
