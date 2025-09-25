using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class MenuMusicPlayer : MonoBehaviour
{
    [SerializeField] AudioMixer mixer;                  // ugyanaz az asset, mint a VolumeManager használ
    [SerializeField] string musicGroupName = "Music";   // a Music csoport neve a Mixerben
    [SerializeField] string[] allowedScenes = { "MainMenu", "PauseMenu" };

    AudioSource src;

    void Awake()
    {
        src = GetComponent<AudioSource>();

        if (src.outputAudioMixerGroup == null && mixer != null)
        {
            var groups = mixer.FindMatchingGroups(musicGroupName);
            if (groups != null && groups.Length > 0)
                src.outputAudioMixerGroup = groups[0];
        }

        src.playOnAwake = false;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(PlayNextFrame());
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HandleScene(scene.name);
    }

    IEnumerator PlayNextFrame()
    {
        yield return null;
        HandleScene(SceneManager.GetActiveScene().name);
    }

    void HandleScene(string sceneName)
    {
        bool shouldPlay = false;
        foreach (var s in allowedScenes)
        {
            if (sceneName == s) { shouldPlay = true; break; }
        }

        if (shouldPlay)
        {
            if (!src.isPlaying)
            {
                src.Play();
            }
        }
        else
        {
            if (src.isPlaying)
            {
                src.Stop();
            }
        }
    }
}
