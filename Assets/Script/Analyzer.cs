using UnityEngine;

public class Analyzer : MonoBehaviour
{
    bool isAnalyzing = false;
    int readyOnDay = 0;

    public void StartAnalyzeOneDay()
    {
        if (isAnalyzing) return;
        isAnalyzing = true;
        readyOnDay = (DayNightSystem.Instance?.CurrentDay ?? 0) + 1;
    }

    void OnEnable()
    {
        if (DayNightSystem.Instance != null)
            DayNightSystem.Instance.OnDayAdvanced += OnDay;
    }
    void OnDisable()
    {
        if (DayNightSystem.Instance != null)
            DayNightSystem.Instance.OnDayAdvanced -= OnDay;
    }

    void OnDay(int d)
    {
        if (isAnalyzing && d >= readyOnDay)
        {
            isAnalyzing = false;
            // TODO: eredmény kiadása
            Debug.Log("[Analyzer] Analysis complete on day " + d);
        }
    }
}
