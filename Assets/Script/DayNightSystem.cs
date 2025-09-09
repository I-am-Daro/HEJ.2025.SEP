using System;
using UnityEngine;

public class DayNightSystem : MonoBehaviour
{
    public static DayNightSystem Instance { get; private set; }

    public int CurrentDay { get; private set; } = 1;

    public event Action<int> OnDayAdvanced; // pl. növények/analizátor feliratkoznak

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AdvanceDay()
    {
        CurrentDay++;
        OnDayAdvanced?.Invoke(CurrentDay);
        Debug.Log($"[DayNight] New day: {CurrentDay}");
    }

    // Opcionális: elsõ mentéshez kívülrõl is állítható
    public void SetDay(int day) => CurrentDay = Mathf.Max(1, day);
}
