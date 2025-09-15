using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerMovementService))]
public class PlayerModeSwitcher : MonoBehaviour
{
    [SerializeField] string exteriorScene = "Planet_Exterior";
    [SerializeField] List<string> interiorScenes = new() { "Ship_Interior", "Greenhouse_Interior" };

    PlayerMovementService svc;

    void Awake() => svc = GetComponent<PlayerMovementService>();

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(ApplyNextFrame()); // biztosak vagyunk benne, hogy induláskor is beáll
    }

    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene s, LoadSceneMode m) => StartCoroutine(ApplyNextFrame());

    IEnumerator ApplyNextFrame()
    {
        yield return null; // várjunk 1 frame-et, hogy minden scene-beli dolog létrejöjjön
        ApplyFor(SceneManager.GetActiveScene().name);
    }

    void ApplyFor(string sceneName)
    {
        bool isInterior = interiorScenes != null && interiorScenes.Contains(sceneName);
        if (isInterior) svc.Apply(MoveMode.InteriorSide);
        else svc.Apply(MoveMode.ExteriorTopDown);
    }
}
