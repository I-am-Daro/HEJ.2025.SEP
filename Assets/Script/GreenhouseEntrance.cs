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

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    public void Interact(PlayerStats player)
    {
        // ÁRAM ELLENŐRZÉS
        var consumer = GetComponentInParent<PowerConsumer>() ?? GetComponent<PowerConsumer>();
        if (!consumer || consumer.node == null || !consumer.node.connectedToSource)
        {
            Debug.Log("[GH Entrance] No power – connect greenhouse to the ship first.");
            return;
        }

        // StabilId kell a TravelContexthez
        var sid = GetComponent<StableId>() ?? GetComponentInParent<StableId>();
        if (!sid || string.IsNullOrEmpty(sid.Id))
        {
            Debug.LogError("[GH Entrance] StableId missing!");
            return;
        }

        TravelContext.currentGreenhouseId = sid.Id;

        // visszatérés beállítások
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
