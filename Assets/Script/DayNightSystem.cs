using System;
using UnityEngine;

public class DayNightSystem : MonoBehaviour
{
    public static DayNightSystem Instance { get; private set; }

    public int CurrentDay { get; private set; } = 1;

    public event Action<int> OnDayAdvanced; // pl. n�v�nyek/analiz�tor feliratkoznak

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

    // Opcion�lis: els� ment�shez k�v�lr�l is �ll�that�
    public void SetDay(int day) => CurrentDay = Mathf.Max(1, day);
}
