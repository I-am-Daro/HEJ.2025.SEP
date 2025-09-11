using UnityEngine;

[CreateAssetMenu(menuName = "Plants/Seed Item")]
public class SeedItem : ScriptableObject
{
    public string displayName;
    public PlantDefinition plant;
    public Sprite icon;
}
