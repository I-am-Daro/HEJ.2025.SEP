using UnityEngine;

[CreateAssetMenu(menuName = "Defs/Building", fileName = "Building_")]
public class BuildingDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    public Sprite icon;                 // BuildMenuUI ezt haszn�lja

    [Header("Prefab")]
    public GameObject prefab;

    [Header("Placement")]
    public Vector2 size = Vector2.one;
    public LayerMask blockMask = ~0;
    public bool canRotate = true;

    // --- Opcion�lis no-build szab�ly ---
    [Header("No-build rule (optional)")]
    public bool forbidNearTagged = false;
    public string nearTag = "Spaceship";
    public float minDistance = 6f;

    // --- K�LTS�G ---
    [Header("Cost")]
    [Tooltip("Mennyibe ker�l Iron-ban a lerak�s (0 = ingyen).")]
    public int ironCost = 0;
}
