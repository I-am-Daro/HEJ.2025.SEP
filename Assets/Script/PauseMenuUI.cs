using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PauseMenuUI : MonoBehaviour
{
    [Header("Panels (parent stays ACTIVE while browsing)")]
    [SerializeField] GameObject pausePanel;     // pl. "EscapePanel"
    [SerializeField] GameObject buttonsRoot;    // Resume/Options/Controls/Main/Quit parent (opcionális)
    [SerializeField] GameObject optionsPanel;   // pl. "Options_panel"
    [SerializeField] GameObject controlsPanel;  // pl. "Controlls_panel" (nálad így íródik)

    [Header("Main Buttons")]
    [SerializeField] Button resumeBtn;
    [SerializeField] Button optionsBtn;
    [SerializeField] Button controlsBtn;
    [SerializeField] Button backToMenuBtn;
    [SerializeField] Button quitBtn;

    [Header("Sub-Panel Back Buttons (optional)")]
    [SerializeField] Button optionsBackBtn;
    [SerializeField] Button controlsBackBtn;

    [Header("Scenes")]
    [SerializeField] string mainMenuScene = "MainMenu";

    bool isPaused;

    void Awake()
    {
        // Auto-find, ha üresen maradt valami
        if (!pausePanel) pausePanel = FindDeep(transform, "EscapePanel");
        if (!optionsPanel) optionsPanel = FindDeep(transform, "Options_panel");
        if (!controlsPanel) controlsPanel = FindDeep(transform, "Controlls_panel");

        if (!buttonsRoot && pausePanel)
        {
            // gyakran van külön "Buttons" nevű child; ha nincs, maradhat null
            var maybeButtons = FindDeep(pausePanel.transform, "Buttons");
            if (maybeButtons) buttonsRoot = maybeButtons;
        }

        // Kezdő láthatóság
        SetActiveSafe(pausePanel, false);
        SetActiveSafe(optionsPanel, false);
        SetActiveSafe(controlsPanel, false);
        SetActiveSafe(buttonsRoot, false);

        // Gomb drótozás
        if (resumeBtn) resumeBtn.onClick.AddListener(Resume);
        if (optionsBtn) optionsBtn.onClick.AddListener(OpenOptions);
        if (controlsBtn) controlsBtn.onClick.AddListener(OpenControls);
        if (backToMenuBtn) backToMenuBtn.onClick.AddListener(BackToMenu);
        if (quitBtn) quitBtn.onClick.AddListener(QuitGame);

        if (optionsBackBtn) optionsBackBtn.onClick.AddListener(CloseOptions);
        if (controlsBackBtn) controlsBackBtn.onClick.AddListener(CloseControls);
    }

    void Update()
    {
        // === BRUTÁL BIZTOS ESC/PAUSE POLLING ===
        bool esc =
            Input.GetKeyDown(KeyCode.Escape)               // régi Input System
#if ENABLE_INPUT_SYSTEM
            || (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)   // új Input System
#endif
            ;

        // Tartalék 'P' (debugra, ha valami mégis interceptálja az ESC-et)
        bool fallbackP =
            Input.GetKeyDown(KeyCode.P)
#if ENABLE_INPUT_SYSTEM
            || (Keyboard.current != null &&
                Keyboard.current.pKey.wasPressedThisFrame)
#endif
            ;

        if (esc || fallbackP)
        {
            if (!isPaused) { Pause(); return; }

            // Paused → Esc: sub-panelről vissza a fő menüre, különben Resume
            if (optionsPanel && optionsPanel.activeSelf) { CloseOptions(); return; }
            if (controlsPanel && controlsPanel.activeSelf) { CloseControls(); return; }
            Resume();
        }
    }

    // ===== Core =====
    public void Pause()
    {
        SetActiveSafe(pausePanel, true);   // SZÜLŐ BE
        SetActiveSafe(buttonsRoot, true);
        SetActiveSafe(optionsPanel, false);
        SetActiveSafe(controlsPanel, false);

        Time.timeScale = 0f;
        isPaused = true;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Resume()
    {
        SetActiveSafe(optionsPanel, false);
        SetActiveSafe(controlsPanel, false);
        SetActiveSafe(buttonsRoot, false);
        SetActiveSafe(pausePanel, false);  // teljes menü zár

        Time.timeScale = 1f;
        isPaused = false;
    }

    // ===== Options =====
    public void OpenOptions()
    {
        SetActiveSafe(pausePanel, true);   // parent marad aktív
        SetActiveSafe(buttonsRoot, false);
        SetActiveSafe(controlsPanel, false);
        SetActiveSafe(optionsPanel, true);
    }

    public void CloseOptions()
    {
        SetActiveSafe(optionsPanel, false);
        SetActiveSafe(buttonsRoot, true);
    }

    // ===== Controls =====
    public void OpenControls()
    {
        SetActiveSafe(pausePanel, true);   // parent marad aktív
        SetActiveSafe(buttonsRoot, false);
        SetActiveSafe(optionsPanel, false);
        SetActiveSafe(controlsPanel, true);
    }

    public void CloseControls()
    {
        SetActiveSafe(controlsPanel, false);
        SetActiveSafe(buttonsRoot, true);
    }

    // ===== Scene / Quit =====
    void BackToMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(mainMenuScene, LoadSceneMode.Single);
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ===== Helpers =====
    static void SetActiveSafe(GameObject go, bool v)
    {
        if (go && go.activeSelf != v) go.SetActive(v);
    }

    static GameObject FindDeep(Transform root, string name)
    {
        if (!root || string.IsNullOrEmpty(name)) return null;
        if (root.name == name) return root.gameObject;
        for (int i = 0; i < root.childCount; i++)
        {
            var r = FindDeep(root.GetChild(i), name);
            if (r) return r;
        }
        return null;
    }
}
