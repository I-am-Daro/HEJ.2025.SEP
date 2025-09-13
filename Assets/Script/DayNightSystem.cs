using UnityEngine;
using System;

public class DayNightSystem : MonoBehaviour
{
    public static DayNightSystem Instance { get; private set; }

    public int CurrentDay { get; private set; } = 0;
    public event Action<int> OnDayAdvanced;

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
        UnityEngine.Debug.Log($"[DayNight] New day: {CurrentDay}");
    }

    // mentés betöltéshez
    public void SetDay(int day, bool invokeEvent = false)
    {
        if (day < 0) day = 0;
        CurrentDay = day;
        if (invokeEvent) OnDayAdvanced?.Invoke(CurrentDay);
        UnityEngine.Debug.Log($"[DayNight] SetDay -> {CurrentDay}{(invokeEvent ? " (invoke)" : "")}");
    }
}
