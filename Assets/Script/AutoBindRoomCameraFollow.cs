using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(500)]
public class AutoBindRoomCameraFollow : MonoBehaviour
{
    [Header("Player & Anchor")]
    [SerializeField] string playerTag = "Player";
    [SerializeField] string anchorName = "CameraAnchor";   // ezt keresi/l�trehozza

    [Header("Cinemachine (auto-detect if empty)")]
    [SerializeField] MonoBehaviour cinemachineComponent;   // CM vcam / CinemachineCamera

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Bind(); // azonnal is pr�b�l
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

        // 2) Anchor keres�se/l�trehoz�sa A VIL�GBAN (NEM a Player alatt!)
        var anchor = FindSceneAnchor(anchorName);
        if (!anchor)
        {
            var go = new GameObject(anchorName);
            anchor = go.transform;
            // kezd�poz�ci�: jelenlegi player poz�ci� (a grid �gyis a szoba k�zep�re fogja snapelni)
            anchor.position = player.transform.position;
        }
        // biztosan ne legyen a Player gyereke
        if (anchor.parent != null) anchor.SetParent(null, true);

        // 3) cameraAnchor mez� be�ll�t�sa reflectionnel (priv�t serialize field)
        var f = typeof(ScreenGridController)
                .GetField("cameraAnchor", BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) f.SetValue(grid, anchor);

        // 4) Cinemachine komponens felkutat�sa (CM2 vagy CM3), �s Follow = anchor
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

        // (opcion�lis) Position Composer Dampinget k�zzel null�zd az Inspectorban,
        // �s kapcsold be a "Center on Activate"-ot, hogy a v�lt�s azonnali legyen.
    }

    Transform FindSceneAnchor(string name)
    {
        // csak root-szinten keres�nk egy ilyet (nem a Player alatt)
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var r in roots)
        {
            var t = r.transform.Find(name);
            if (t && t.parent == r.transform) return t; // k�zvetlen gyerek
            if (r.name == name) return r.transform;
        }
        // fallback: b�rhol a jelenetben
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
