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
        // pr�b�ljunk azonnal is bindolni (Play k�zben scene v�lt�s n�lk�l)
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
            // ha nincs, l�trehozzuk
            var go = new GameObject(anchorName);
            anchor = go.transform;
            anchor.SetParent(player.transform, false);
        }

        // 2) Cinemachine komponens felkutat�sa, ha nincs k�zzel megadva
        var cm = cinemachineComponent ? cinemachineComponent : FindCinemachineComponent();
        if (!cm)
        {
            Debug.LogWarning("[AutoBindCameraFollow] No Cinemachine component found in scene.");
            return;
        }

        // 3) Follow property be�ll�t�sa reflectionnel (CM2 �s CM3 is 'Follow' nev� Transform property-t haszn�l)
        var ok = TrySetFollow(cm, anchor);
        if (!ok)
        {
            Debug.LogWarning($"[AutoBindCameraFollow] Could not set Follow on component type {cm.GetType().Name}");
        }
        else
        {
            // (opcion�lis) pr�b�ljuk kikapcsolni a Position Dampinget, ha van ilyen komponens
            TryZeroPositionDamping(cm.gameObject);
        }
    }

    MonoBehaviour FindCinemachineComponent()
    {
        // 1) el�sz�r n�zz�k a saj�t GameObjecten
        var local = GetCinemachineOn(gameObject);
        if (local) return local;

        // 2) k�l�n CM riget keres�nk a jelenetben
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
        // CM2 �s CM3 egyar�nt 'Follow' Transform property
        var prop = type.GetProperty("Follow", BindingFlags.Public | BindingFlags.Instance);
        if (prop != null && prop.PropertyType == typeof(Transform) && prop.CanWrite)
        {
            prop.SetValue(cm, target);
            return true;
        }
        return false;
    }

    // Pr�b�ljuk 0-ra �ll�tani a position dampinget CM komponenseken, ha megtal�ljuk �ket (opcion�lis)
    void TryZeroPositionDamping(GameObject cmGO)
    {
        // CM3: "CinemachinePositionComposer"
        ZeroDampingOn(cmGO, "CinemachinePositionComposer", "Damping");
        // CM2 (deprecated komponensek): nincs egys�ges mez�n�v, ez�rt kihagyhat�.
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
                // ha a t�pus sz�msorozat/struct, ezt nem piszk�ljuk bonyolults�g miatt
                // (Inspectorban egyszer �ll�tsd 0-ra �s k�sz)
                return;
            }
            var f = t.GetField(fieldOrProp, BindingFlags.Public | BindingFlags.Instance);
            if (f != null) return;
        }
    }
}
