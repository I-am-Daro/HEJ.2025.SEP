using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class ScenePortal : MonoBehaviour
{
    [SerializeField] private string targetScene;
    [SerializeField] private string targetSpawnId = "Default";

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true; // automatikus bekapcs a komfort kedvéért
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError($"[ScenePortal] targetScene nincs beállítva a(z) {name} objektumon.");
            return;
        }

        SpawnPoint.NextSpawnId = targetSpawnId;
        Debug.Log($"[ScenePortal] Belépés: {other.name} -> töltöm: {targetScene}, spawn: {targetSpawnId}");
        SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
    }
}
