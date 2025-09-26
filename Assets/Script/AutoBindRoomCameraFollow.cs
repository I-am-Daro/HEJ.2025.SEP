using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(500)]
public class AutoBindRoomCameraFollow : MonoBehaviour
{
    [Header("Player & Anchor")]
    [SerializeField] string playerTag = "Player";
    [SerializeField] string anchorName = "CameraAnchor";   // ezt keresi/létrehozza

    [Header("Cinemachine (auto-detect if empty)")]
    [SerializeField] MonoBehaviour cinemachineComponent;   // CM vcam / CinemachineCamera

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Bind(); // azonnal is próbál
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m) => Bind();

    void Bind()
    {
        var player = GameObject.FindGameObjectWithTag(playerTag);
        if (!player) return;

        // 1) ScreenGridController a Playeren
        var grid = player.GetComponent<ScreenGridController>();
        if (!grid)
        {
            Debug.LogWarning("[AutoBindRoomCameraFollow] No ScreenGridController on Player.");
            return;
        }

        // 2) Anchor keresése/létrehozása A VILÁGBAN (NEM a Player alatt!)
        var anchor = FindSceneAnchor(anchorName);
        if (!anchor)
        {
            var go = new GameObject(anchorName);
            anchor = go.transform;
            // kezdõpozíció: jelenlegi player pozíció (a grid úgyis a szoba közepére fogja snapelni)
            anchor.position = player.transform.position;
        }
        // biztosan ne legyen a Player gyereke
        if (anchor.parent != null) anchor.SetParent(null, true);

        // 3) cameraAnchor mezõ beállítása reflectionnel (privát serialize field)
        var f = typeof(ScreenGridController)
                .GetField("cameraAnchor", BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) f.SetValue(grid, anchor);

        // 4) Cinemachine komponens felkutatása (CM2 vagy CM3), és Follow = anchor
        var cm = cinemachineComponent ? cinemachineComponent : FindCinemachineComponent();
        if (!cm)
        {
            Debug.LogWarning("[AutoBindRoomCameraFollow] No Cinemachine component found.");
            return;
        }

        var followProp = cm.GetType().GetProperty("Follow", BindingFlags.Public | BindingFlags.Instance);
        if (followProp != null && followProp.PropertyType == typeof(Transform) && followProp.CanWrite)
        {
            followProp.SetValue(cm, anchor);
        }
        else
        {
            Debug.LogWarning($"[AutoBindRoomCameraFollow] Could not set Follow on {cm.GetType().Name}");
        }

        // (opcionális) Position Composer Dampinget kézzel nullázd az Inspectorban,
        // és kapcsold be a "Center on Activate"-ot, hogy a váltás azonnali legyen.
    }

    Transform FindSceneAnchor(string name)
    {
        // csak root-szinten keresünk egy ilyet (nem a Player alatt)
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var r in roots)
        {
            var t = r.transform.Find(name);
            if (t && t.parent == r.transform) return t; // közvetlen gyerek
            if (r.name == name) return r.transform;
        }
        // fallback: bárhol a jelenetben
        var any = GameObject.Find(name);
        return any ? any.transform : null;
    }

    MonoBehaviour FindCinemachineComponent()
    {
        foreach (var mb in FindObjectsOfType<MonoBehaviour>(true))
        {
            if (!mb) continue;
            var n = mb.GetType().Name;
            if (n == "CinemachineVirtualCamera" || n == "CinemachineCamera")
                return mb;
        }
        return null;
    }
}
