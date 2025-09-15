using UnityEngine;

[CreateAssetMenu(menuName = "Defs/Building")]
public class BuildingDefinition : ScriptableObject
{
    public string id;                 // pl. "greenhouse"
    public string displayName;
    public Sprite icon;

    public GameObject prefab;
    public Vector2 size = Vector2.one;
    public bool canRotate = true;
    public bool requiresPower = false;
    public bool isPipeSegment = false;

    public LayerMask blockMask;
}
