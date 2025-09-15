using UnityEngine;

[CreateAssetMenu(menuName = "Plants/Plant Definition")]
public class PlantDefinition : ScriptableObject
{
    [Header("IDs")]
    public string id;
    public string displayName;

    [Header("Stages (days)")]
    public int daysSeedToSapling = 1;
    public int daysSaplingToMature = 1;
    public int daysMatureToFruiting = 1;
    public int regrowDaysAfterHarvest = 3;

    [Header("Sprites")]
    public Sprite seedSprite;
    public Sprite saplingSprite;
    public Sprite matureSprite;
    public Sprite fruitingSprite;
    public Sprite witheredSprite;

    [Header("Produce")]
    public ProduceType produceType;  // <- az enumot már a külön fájlból veszi
    public int produceAmount = 1;
}
