using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnPoint : MonoBehaviour
{
    public static string NextSpawnId = "Default";
    [SerializeField] private string spawnId = "Default";

    private void OnEnable()
    {
        // ha a Player már a scene-ben van, azonnal próbálunk spawnt
        TryPlacePlayer();

        // ha valamiért a Player késõbb kerül be, jelenet betöltés után is próbáljuk
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryPlacePlayer();
    }

    private void TryPlacePlayer()
    {
        if (spawnId != NextSpawnId) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning($"[SpawnPoint:{spawnId}] Nincs Player a scene-ben.");
            return;
        }

        player.transform.position = transform.position;
        Debug.Log($"[SpawnPoint:{spawnId}] Player áthelyezve ide: {transform.position}");
    }
}
