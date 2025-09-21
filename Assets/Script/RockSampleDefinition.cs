using UnityEngine;

public enum ResourceType
{
    Iron,
    Water,
}

[CreateAssetMenu(menuName = "Defs/Rock Sample", fileName = "RockSample_")]
public class RockSampleDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    public Sprite icon;

    [Header("Analysis time (days)")]
    [Min(0)] public int analysisDaysMin = 1;
    [Min(0)] public int analysisDaysMax = 2;

    [System.Serializable]
    public struct ResourceYield
    {
        public ResourceType type;
        public int min;   // inclusive
        public int max;   // inclusive
    }

    [Header("Guaranteed/rolled resource yields")]
    public ResourceYield[] yields;

    [Header("Optional seed drop")]
    public PlantDefinition seedDef;
    [Range(0, 100)] public int seedChancePercent = 0;
    [Min(0)] public int seedMin = 1;
    [Min(0)] public int seedMax = 1;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (analysisDaysMax < analysisDaysMin)
            analysisDaysMax = analysisDaysMin;

        if (yields != null)
        {
            for (int i = 0; i < yields.Length; i++)
            {
                if (yields[i].max < yields[i].min)
                    yields[i].max = yields[i].min;
            }
        }
        if (seedMax < seedMin) seedMax = seedMin;
    }
#endif
}
