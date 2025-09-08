using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnPoint : MonoBehaviour
{
    public static string NextSpawnId = "Default";
    [SerializeField] private string spawnId = "Default";

    private void OnEnable()
    {
        // ha a Player m�r a scene-ben van, azonnal pr�b�lunk spawnt
        TryPlacePlayer();

        // ha valami�rt a Player k�s�bb ker�l be, jelenet bet�lt�s ut�n is pr�b�ljuk
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
        Debug.Log($"[SpawnPoint:{spawnId}] Player �thelyezve ide: {transform.position}");
    }
}
