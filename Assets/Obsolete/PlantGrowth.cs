using UnityEngine;

public class PlantGrowth : MonoBehaviour
{
    public enum Stage { Seed, Sapling, Mature, Fruiting }
    [SerializeField] Stage stage = Stage.Seed;
    [SerializeField] int daysToNext = 1;

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

    void OnDay(int newDay)
    {
        // Itt n�veked�si logika (�nt�z�s felt�tel�t majd ide k�t�d)
        daysToNext--;
        if (daysToNext <= 0)
        {
            AdvanceStage();
        }
    }

    void AdvanceStage()
    {
        // egyszer� p�lda
        if (stage == Stage.Seed) { stage = Stage.Sapling; daysToNext = 1; }
        else if (stage == Stage.Sapling) { stage = Stage.Mature; daysToNext = 1; }
        else if (stage == Stage.Mature) { stage = Stage.Fruiting; daysToNext = 3; }
        else if (stage == Stage.Fruiting) { stage = Stage.Mature; daysToNext = 3; }
        // TODO: sprite v�lt�s, stb.
    }
}
