using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class GreenhouseExit : MonoBehaviour
{
    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (string.IsNullOrEmpty(TravelContext.returnScene))
        {
            Debug.LogError("[GreenhouseExit] Missing TravelContext.returnScene.");
            return;
        }

        if (TravelContext.useWorldPosition)
        {
            // Világpozícióra vissza
            var sceneName = TravelContext.returnScene;
            SceneManager.sceneLoaded += OnLoaded;
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            // SpawnPointtal vissza
            SpawnPoint.NextSpawnId = TravelContext.returnSpawnId;
            SceneManager.LoadScene(TravelContext.returnScene);
        }
    }

    void OnLoaded(Scene s, LoadSceneMode m)
    {
        SceneManager.sceneLoaded -= OnLoaded;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            player.transform.position = TravelContext.returnWorldPos;
        }
        else
        {
            Debug.LogError("[GreenhouseExit] Player not found after load.");
        }
    }
}
