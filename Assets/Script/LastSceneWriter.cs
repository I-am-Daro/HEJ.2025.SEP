using UnityEngine;
using UnityEngine.SceneManagement;

public class LastSceneWriter : MonoBehaviour
{
    void OnEnable()
    {
        var s = SceneManager.GetActiveScene();
        if (s.IsValid())
            PlayerPrefs.SetString("LastScene", s.name);
    }
}
