using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerMovementService))]
public class PlayerModeSwitcher : MonoBehaviour
{
    [SerializeField] string exteriorScene = "Planet_Exterior";
    [SerializeField] string interiorScene = "Ship_Interior";

    PlayerMovementService svc;

    void Awake() => svc = GetComponent<PlayerMovementService>();
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Apply();
    }
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    void OnSceneLoaded(Scene s, LoadSceneMode m) => Apply();

    void Apply()
    {
        string s = SceneManager.GetActiveScene().name;
        if (s == interiorScene)
            svc.Apply(MoveMode.InteriorSide);
        else
            svc.Apply(MoveMode.ExteriorTopDown);
    }
}
