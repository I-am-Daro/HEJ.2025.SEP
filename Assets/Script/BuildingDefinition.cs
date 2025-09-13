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
    public Vector2 size = Vector2.one;    // overlapCheck doboz f�lm�ret = size/2
    public bool canRotate = true;
    public LayerMask blockMask;           // mivel ne �tk�zz�n (pl. Walls, Buildings)
    public bool requiresPower = true;     // kell-e h�l�zat
    public bool isPipeSegment = false;    // ha true, ez cs�elem

    [Header("Costs (opcion�lis)")]
    public int oxygenCost;
    public int foodCost;
}
