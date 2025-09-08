using UnityEngine;

public class PersistAcrossScenes : MonoBehaviour
{
    private static bool created;
    void Awake()
    {
        if (created) { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
        created = true;
    }
}
