using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class GreenhouseExit : MonoBehaviour, IInteractable
{
    [Header("Target exterior scene name (exact)")]
    [SerializeField] string exteriorSceneName = "Planet_Exterior";

    [SerializeField] string fallbackSpawnId = "FromGreenhouse_GH"; // csak ha NINCS world-pos

    public string GetPrompt() => "Exit Greenhouse";

    public void Interact(PlayerStats player)
    {
        if (!player) return;

        // Kanna guard: amíg nálad van, nem léphetsz ki
        var inv = player.GetComponent<PlayerInventory>();
        if (inv && inv.HasWateringCan)
        {
            Debug.Log("[GreenhouseExit] Tedd vissza a locsolókannát, mielőtt kimennél.");
            return;
        }

        // VISSZAÚT: ha belépéskor eltároltunk world-positiont, akkor azt használjuk.
        // (Ezt a GreenhouseEntrance már beállította.)
        bool haveWorldReturn = TravelContext.useWorldPosition;

        // Ha mégsem lenne world-pos (ritka), essen vissza named spawnra:
        if (!haveWorldReturn)
            SpawnPoint.NextSpawnId = fallbackSpawnId;
        else
            SpawnPoint.NextSpawnId = null; // ne írja felül a world-pos-t

        if (string.IsNullOrEmpty(exteriorSceneName))
        {
            Debug.LogError("[GreenhouseExit] exteriorSceneName nincs beállítva!");
            return;
        }

        Debug.Log($"[GreenhouseExit] Loading scene '{exteriorSceneName}' (worldPos={(haveWorldReturn ? "YES" : "NO")})");
        SceneManager.LoadScene(exteriorSceneName);
    }
}
