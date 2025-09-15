using UnityEngine;

[DisallowMultipleComponent]
public class StableId : MonoBehaviour
{
    [SerializeField] string id;
    public string Id => id;

    /// Ha �res az id, gener�l egyet (csak fut�s k�zben h�vjuk automatikusan)
    public string EnsureId()
    {
        if (string.IsNullOrEmpty(id))
            id = System.Guid.NewGuid().ToString("N");
        return id;
    }

    /// Kifejezetten �j, egyedi ID-t ad (prefab instance-okn�l lerak�skor h�vd!)
    public void AssignNewRuntimeId()
    {
        id = System.Guid.NewGuid().ToString("N");
    }

    /// Vissza�ll�t�s ment�sb�l
    public void SetIdForRestore(string savedId)
    {
        if (!string.IsNullOrEmpty(savedId))
            id = savedId;
    }

    void Awake()
    {
        // Csak fut�s k�zben gener�lunk automatikusan;
        // szerkeszt�s alatt NEM �runk bele a prefab assetbe.
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
