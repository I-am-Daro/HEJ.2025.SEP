using UnityEngine;

[DisallowMultipleComponent]
public class StableId : MonoBehaviour
{
    [SerializeField] string id;
    public string Id => id;

    /// Ha üres az id, generál egyet (csak futás közben hívjuk automatikusan)
    public string EnsureId()
    {
        if (string.IsNullOrEmpty(id))
            id = System.Guid.NewGuid().ToString("N");
        return id;
    }

    /// Kifejezetten új, egyedi ID-t ad (prefab instance-oknál lerakáskor hívd!)
    public void AssignNewRuntimeId()
    {
        id = System.Guid.NewGuid().ToString("N");
    }

    /// Visszaállítás mentésbõl
    public void SetIdForRestore(string savedId)
    {
        if (!string.IsNullOrEmpty(savedId))
            id = savedId;
    }

    void Awake()
    {
        // Csak futás közben generálunk automatikusan;
        // szerkesztés alatt NEM írunk bele a prefab assetbe.
        if (Application.isPlaying)
            EnsureId();
    }

    public static StableId AddTo(GameObject go)
    {
        var s = go.GetComponent<StableId>();
        if (!s) s = go.AddComponent<StableId>();
        return s;
    }
}
