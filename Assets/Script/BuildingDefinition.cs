using UnityEngine;

[CreateAssetMenu(menuName = "Defs/Building", fileName = "Building_")]
public class BuildingDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    public Sprite icon;                 // BuildMenuUI ezt használja

    [Header("Prefab")]
    public GameObject prefab;

    [Header("Placement")]
    public Vector2 size = Vector2.one;
    public LayerMask blockMask = ~0;
    public bool canRotate = true;

    // --- Opcionális no-build szabály ---
    [Header("No-build rule (optional)")]
    public bool forbidNearTagged = false;
    public string nearTag = "Spaceship";
    public float minDistance = 6f;

    // --- KÖLTSÉG ---
    [Header("Cost")]
    [Tooltip("Mennyibe kerül Iron-ban a lerakás (0 = ingyen).")]
    public int ironCost = 0;
}
