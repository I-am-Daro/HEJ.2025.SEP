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
        col.isTrigger = true; // automatikus bekapcs a komfort kedv��rt
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError($"[ScenePortal] targetScene nincs be�ll�tva a(z) {name} objektumon.");
            return;
        }

        SpawnPoint.NextSpawnId = targetSpawnId;
        Debug.Log($"[ScenePortal] Bel�p�s: {other.name} -> t�lt�m: {targetScene}, spawn: {targetSpawnId}");
        SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
    }
}
