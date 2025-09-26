using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(500)]
public class AutoBindCameraFollow : MonoBehaviour
{
    [Header("What to follow")]
    [SerializeField] string playerTag = "Player";
    [SerializeField] string anchorName = "Ship_area";

    [Header("Optional: assign CM component here")]
    [Tooltip("Leave empty to auto-detect. Can be CinemachineVirtualCamera (CM2) or CinemachineCamera (CM3).")]
    [SerializeField] MonoBehaviour cinemachineComponent;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        // próbáljunk azonnal is bindolni (Play közben scene váltás nélkül)
        Bind();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m) => Bind();

    void Bind()
    {
        // 1) Player + anchor
        var player = GameObject.FindGameObjectWithTag(playerTag);
        if (!player) return;

        var anchor = player.transform.Find(anchorName);
        if (!anchor)
        {
            // ha nincs, létrehozzuk
            var go = new GameObject(anchorName);
            anchor = go.transform;
            anchor.SetParent(player.transform, false);
        }

        // 2) Cinemachine komponens felkutatása, ha nincs kézzel megadva
        var cm = cinemachineComponent ? cinemachineComponent : FindCinemachineComponent();
        if (!cm)
        {
            Debug.LogWarning("[AutoBindCameraFollow] No Cinemachine component found in scene.");
            return;
        }

        // 3) Follow property beállítása reflectionnel (CM2 és CM3 is 'Follow' nevû Transform property-t használ)
        var ok = TrySetFollow(cm, anchor);
        if (!ok)
        {
            Debug.LogWarning($"[AutoBindCameraFollow] Could not set Follow on component type {cm.GetType().Name}");
        }
        else
        {
            // (opcionális) próbáljuk kikapcsolni a Position Dampinget, ha van ilyen komponens
            TryZeroPositionDamping(cm.gameObject);
        }
    }

    MonoBehaviour FindCinemachineComponent()
    {
        // 1) elõször nézzük a saját GameObjecten
        var local = GetCinemachineOn(gameObject);
        if (local) return local;

        // 2) külön CM riget keresünk a jelenetben
        foreach (var mb in FindObjectsOfType<MonoBehaviour>(true))
        {
            if (!mb) continue;
            var t = mb.GetType().Name;
            if (t == "CinemachineVirtualCamera" || t == "CinemachineCamera")
                return mb;
        }
        return null;
    }

    MonoBehaviour GetCinemachineOn(GameObject go)
    {
        var mbs = go.GetComponents<MonoBehaviour>();
        foreach (var mb in mbs)
        {
            if (!mb) continue;
            var n = mb.GetType().Name;
            if (n == "CinemachineVirtualCamera" || n == "CinemachineCamera")
                return mb;
        }
        return null;
    }

    bool TrySetFollow(MonoBehaviour cm, Transform target)
    {
        var type = cm.GetType();
        // CM2 és CM3 egyaránt 'Follow' Transform property
        var prop = type.GetProperty("Follow", BindingFlags.Public | BindingFlags.Instance);
        if (prop != null && prop.PropertyType == typeof(Transform) && prop.CanWrite)
        {
            prop.SetValue(cm, target);
            return true;
        }
        return false;
    }

    // Próbáljuk 0-ra állítani a position dampinget CM komponenseken, ha megtaláljuk õket (opcionális)
    void TryZeroPositionDamping(GameObject cmGO)
    {
        // CM3: "CinemachinePositionComposer"
        ZeroDampingOn(cmGO, "CinemachinePositionComposer", "Damping");
        // CM2 (deprecated komponensek): nincs egységes mezõnév, ezért kihagyható.
    }

    void ZeroDampingOn(GameObject go, string componentTypeName, string fieldOrProp)
    {
        var comps = go.GetComponents<MonoBehaviour>();
        foreach (var c in comps)
        {
            if (!c) continue;
            if (c.GetType().Name != componentTypeName) continue;

            var t = c.GetType();
            var p = t.GetProperty(fieldOrProp, BindingFlags.Public | BindingFlags.Instance);
            if (p != null)
            {
                // ha a típus számsorozat/struct, ezt nem piszkáljuk bonyolultság miatt
                // (Inspectorban egyszer állítsd 0-ra és kész)
                return;
            }
            var f = t.GetField(fieldOrProp, BindingFlags.Public | BindingFlags.Instance);
            if (f != null) return;
        }
    }
}
