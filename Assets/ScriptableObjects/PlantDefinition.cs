using UnityEngine;

[CreateAssetMenu(menuName = "Plants/Plant Definition")]
public class PlantDefinition : ScriptableObject
{
    public string id;
    public string displayName;

    [Header("Sprites per stage")]
    public Sprite seedSprite;
    public Sprite saplingSprite;
    public Sprite matureSprite;
    public Sprite fruitingSprite;
    public Sprite witheredSprite;

    [Header("Growth (days)")]
    public int daysSeedToSapling = 1;
    public int daysSaplingToMature = 1;
    public int daysMatureToFruiting = 1;
    public int regrowDaysAfterHarvest = 3;

    [Header("Yield")]
    public ProduceType produceType = ProduceType.Food;
    public int produceAmount = 1;
}
