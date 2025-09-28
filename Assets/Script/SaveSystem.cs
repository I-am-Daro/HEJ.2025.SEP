using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UObject = UnityEngine.Object;

public static class SaveSystem
{
    static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    // ==== �J: ide tessz�k a men�ben kikapcsolt Player ref-j�t ====
    static GameObject cachedDeactivatedPlayer;
    public static void CacheDeactivatedPlayer(GameObject go) => cachedDeactivatedPlayer = go;

    public static bool HasSave() => File.Exists(SavePath);
    public static void DeleteSave() { if (File.Exists(SavePath)) File.Delete(SavePath); }

    public static void Save(GameSave data)
    {
        if (string.IsNullOrEmpty(data.sceneName))
            data.sceneName = SceneManager.GetActiveScene().name;
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

        Time.timeScale = 1f;

        var current = SceneManager.GetActiveScene().name;
        var targetScene = string.IsNullOrEmpty(save.sceneName) ? current : save.sceneName;

        if (targetScene != current)
        {
            SceneManager.sceneLoaded += OnLoaded;
            SceneManager.LoadScene(targetScene);
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
        // 1) Els�k�nt a cache-elt (men�ben kikapcsolt) Player p�ld�nyt pr�b�ljuk haszn�lni
        GameObject player = cachedDeactivatedPlayer;

        // 2) Ha nincs cache, keress tag alapj�n (akt�v objektumok k�zt)
        if (!player)
            player = GameObject.FindGameObjectWithTag("Player");

        // 3) Ha m�g mindig nincs, keress�k komponenst inakt�vak k�zt is (2022.3+)
#if UNITY_2022_3_OR_NEWER
        if (!player)
        {
            var stats = UnityEngine.Object.FindFirstObjectByType<PlayerStats>(UnityEngine.FindObjectsInactive.Include);
            if (stats) player = stats.gameObject;
        }
#else
        if (!player)
        {
            var stats = UnityEngine.Object.FindObjectOfType<PlayerStats>(true); // includeInactive
            if (stats) player = stats.gameObject;
        }
#endif

        // 4) Ha semmi sem volt, itt d�nthetsz: hiba vagy spawn Resources-b�l
        if (!player)
        {
            var prefab = Resources.Load<GameObject>("Player"); // Resources/Player.prefab (ha van)
            if (prefab) player = UnityEngine.Object.Instantiate(prefab);
            else { Debug.LogError("[Save] Player not found (no cached, no scene, no prefab)."); return; }
        }

        // Tiszt�tsuk a cache-t � mostant�l �l a p�ld�ny
        cachedDeactivatedPlayer = null;

        // 5) Aktiv�ld vissza, �s enged�lyezd a fizik�t
        if (!player.activeSelf) player.SetActive(true);

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb)
        {
#if UNITY_2022_3_OR_NEWER
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
#else
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
#endif
            rb.simulated = true;
        }

        // 6) Poz�ci� + statok + nap
        player.transform.position = save.playerPosition;

        var statsComp = player.GetComponent<PlayerStats>();
        if (statsComp)
        {
            statsComp.oxygen = save.oxygen;
            statsComp.energy = save.energy;
            statsComp.hunger = save.hunger;
        }

        if (DayNightSystem.Instance)
            DayNightSystem.Instance.SetDay(save.day);

        // 7) Mozg�s-komponensek visszakapcsol�sa
        SceneMovementBoot boot = null;
#if UNITY_2022_3_OR_NEWER
boot = UnityEngine.Object.FindFirstObjectByType<SceneMovementBoot>(UnityEngine.FindObjectsInactive.Include);
#else
        boot = UnityEngine.Object.FindObjectOfType<SceneMovementBoot>(true);
#endif

        if (boot)
        {
            boot.ApplyNow(player);
        }
        else
        {
            // Fallback: ha valami�rt nincs Boot a scene-ben, legal�bb a m�dot �ll�tsuk be.
            var move = player.GetComponent<PlayerMovementService>();
            if (move)
            {
                var kind = SceneMovementBoot.CurrentSceneKind;
                move.SetSceneAuthority(kind);
                move.Apply(kind == SceneKind.Interior ? MoveMode.InteriorSide : MoveMode.ExteriorTopDown);
            }
        }

        Debug.Log($"[Save] Restored: Day {save.day}, Pos {save.playerPosition}");
    }
}