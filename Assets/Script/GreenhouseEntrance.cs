using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class GreenhouseEntrance : MonoBehaviour, IInteractable
{
    [SerializeField] string interiorScene = "Greenhouse_Interior";
    [SerializeField] string interiorSpawnId = "FromExterior_GH";
    [SerializeField] bool returnByWorldPosition = true;
    [SerializeField] string exteriorSpawnId = "FromGreenhouse_GH";

    public string GetPrompt() => "Enter Greenhouse";

    public void Interact(PlayerStats player)
    {
        var sid = GetComponent<StableId>();
        if (!sid || string.IsNullOrEmpty(sid.Id))
        {
            UnityEngine.Debug.LogError("[GH Entrance] StableId missing!");
            return;
        }

        TravelContext.currentGreenhouseId = sid.Id;   // <-- FONTOS

        TravelContext.returnScene = SceneManager.GetActiveScene().name;
        TravelContext.interiorSpawnId = interiorSpawnId;

        if (returnByWorldPosition)
        {
            TravelContext.useWorldPosition = true;
            TravelContext.returnWorldPos = transform.position;
        }
        else
        {
            TravelContext.useWorldPosition = false;
            TravelContext.returnSpawnId = exteriorSpawnId;
        }

        SpawnPoint.NextSpawnId = interiorSpawnId;
        SceneManager.LoadScene(interiorScene);
    }
}
