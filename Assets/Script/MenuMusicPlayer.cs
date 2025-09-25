using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MenuMusicPlayer : MonoBehaviour
{
    AudioSource src;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false; // csak mi ind�tsuk
    }

    void OnEnable()
    {
        StartCoroutine(PlayWhenBootReady());
    }

    System.Collections.IEnumerator PlayWhenBootReady()
    {
        // v�rjuk meg, hogy AudioVolumeBoot biztosan be�ll�tson
        AudioVolumeBoot boot = null;
        while (boot == null)
        {
            boot = FindFirstObjectByType<AudioVolumeBoot>(FindObjectsInactive.Include);
            yield return null;
        }

        // amikor k�sz, ind�tsuk a zen�t
        if (src && !src.isPlaying)
            src.Play();
    }
}
