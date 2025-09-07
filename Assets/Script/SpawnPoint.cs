using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public static string NextSpawnId = "Default";
    [SerializeField] private string spawnId = "Default";

    private void Start()
    {
        if (spawnId == NextSpawnId)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = transform.position;
            }
        }
    }
}
