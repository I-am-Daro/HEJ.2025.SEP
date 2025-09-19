using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class ScenePortalInteract : MonoBehaviour, IInteractable
{
    [Header("Target")]
    [SerializeField] private string targetScene;
    [SerializeField] private string targetSpawnId = "Default";

    [Header("UI prompt")]
    [SerializeField] private string promptText = "Enter";

    // opcionális: tiltás (pl. ha valami feltétel kéne később)
    [SerializeField] private bool requireNotNullTarget = true;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true; // csak promptot ad, nem teleportál automatikusan
    }

    // IInteractable
    public string GetPrompt() => string.IsNullOrEmpty(promptText) ? "Enter" : promptText;

    public void Interact(PlayerStats player)
    {
        if (requireNotNullTarget && string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError($"[ScenePortalInteract] targetScene nincs beállítva a(z) {name} objektumon.");
            return;
        }

        // SpawnPoint rendszer marad: csak a következő spawn ID-t állítjuk.
        SpawnPoint.NextSpawnId = targetSpawnId;

        Debug.Log($"[ScenePortalInteract] Interact → load '{targetScene}', spawn='{targetSpawnId}'");
        SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
    }
}
