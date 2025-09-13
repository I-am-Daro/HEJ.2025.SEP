using UnityEngine;

[CreateAssetMenu(menuName = "Build/Building Definition")]
public class BuildingDefinition : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite icon;

    [Header("Prefab to place")]
    public GameObject prefab;

    [Header("Placement")]
    public Vector2 size = Vector2.one;    // overlapCheck doboz félméret = size/2
    public bool canRotate = true;
    public LayerMask blockMask;           // mivel ne ütközzön (pl. Walls, Buildings)
    public bool requiresPower = true;     // kell-e hálózat
    public bool isPipeSegment = false;    // ha true, ez csõelem

    [Header("Costs (opcionális)")]
    public int oxygenCost;
    public int foodCost;
}
