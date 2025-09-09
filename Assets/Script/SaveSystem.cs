using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveSystem
{
    static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static void Save(GameSave data)
    {
        var json = JsonUtility.ToJson(data);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[Save] Saved to: {SavePath}");
    }

    public static bool TryLoad(out GameSave data)
    {
        if (!File.Exists(SavePath)) { data = null; return false; }
        var json = File.ReadAllText(SavePath);
        data = JsonUtility.FromJson<GameSave>(json);
        return data != null;
    }

    public static void LoadCheckpointAndPlacePlayer()
    {
        if (!TryLoad(out var save))
        {
            Debug.LogWarning("[Save] No checkpoint found.");
            return;
        }

        // ha másik scene-ben volt a mentés, elõbb töltsük be azt
        var current = SceneManager.GetActiveScene().name;
        if (save.sceneName != current)
        {
            SceneManager.sceneLoaded += OnLoaded;
            SceneManager.LoadScene(save.sceneName);
            return;

            void OnLoaded(Scene s, LoadSceneMode m)
            {
                SceneManager.sceneLoaded -= OnLoaded;
                ApplySave(save);
            }
        }

        ApplySave(save);
    }

    static void ApplySave(GameSave save)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) { Debug.LogError("[Save] Player not found in scene."); return; }

        player.transform.position = save.playerPosition;

        var stats = player.GetComponent<PlayerStats>();
        if (stats)
        {
            stats.oxygen = save.oxygen;
            stats.energy = save.energy;
            stats.hunger = save.hunger;
        }

        if (DayNightSystem.Instance) DayNightSystem.Instance.SetDay(save.day);

        Debug.Log($"[Save] Restored: Day {save.day}, Pos {save.playerPosition}");
    }
}
