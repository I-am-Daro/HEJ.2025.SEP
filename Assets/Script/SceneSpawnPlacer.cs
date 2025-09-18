using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSpawnPlacer : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Név szerinti spawn-t a SpawnPoint-ok intézik (NextSpawnId alapján).
        // World-pos visszahelyezést CSAK akkor csináljunk, ha:
        //  - kértük (useWorldPosition == true), ÉS
        //  - a betöltött scene az, ahonnan beléptünk (returnScene).
        if (!TravelContext.useWorldPosition) return;
        if (string.IsNullOrEmpty(TravelContext.returnScene)) return;
        if (scene.name != TravelContext.returnScene) return;

        StartCoroutine(PlaceAtWorldPosEndOfFrame());
    }

    IEnumerator PlaceAtWorldPosEndOfFrame()
    {
        yield return new WaitForEndOfFrame(); // hagyjuk lefutni a SpawnPoint-okat is

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (!playerGO)
        {
            Debug.LogWarning("[SceneSpawnPlacer] Player not found for world-pos placement.");
            yield break;
        }

        Vector3 target = TravelContext.returnWorldPos; target.z = 0f;

        var rb = playerGO.GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = target; // fizikabarát teleport
        }
        else
        {
            playerGO.transform.position = target;
        }

        Debug.Log($"[SceneSpawnPlacer] World-pos place OK: {target} in scene '{SceneManager.GetActiveScene().name}'");

        // egyszer használatos
        TravelContext.useWorldPosition = false;
    }
}
