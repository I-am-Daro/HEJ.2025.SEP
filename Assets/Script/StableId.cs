using UnityEngine;

public class StableId : MonoBehaviour
{
    [SerializeField] string id;
    public string Id => id;

#if UNITY_EDITOR
    [ContextMenu("Generate New ID")]
    void Generate()
    {
        id = System.Guid.NewGuid().ToString("N");
        UnityEngine.Debug.Log($"[StableId] New id on {name}: {id}");
    }
#endif
}
