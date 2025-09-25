using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MenuMusicPlayer : MonoBehaviour
{
    AudioSource src;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false; // csak mi indítsuk
    }

    void OnEnable()
    {
        StartCoroutine(PlayWhenBootReady());
    }

    System.Collections.IEnumerator PlayWhenBootReady()
    {
        // várjuk meg, hogy AudioVolumeBoot biztosan beállítson
        AudioVolumeBoot boot = null;
        while (boot == null)
        {
            boot = FindFirstObjectByType<AudioVolumeBoot>(FindObjectsInactive.Include);
            yield return null;
        }

        // amikor kész, indítsuk a zenét
        if (src && !src.isPlaying)
            src.Play();
    }
}
