using UnityEngine;

[CreateAssetMenu(fileName = "IronItem", menuName = "Items/Iron Item")]
public class IronItem : ScriptableObject
{
    public string id = "IRON";
    public string displayName = "Iron";
    public Sprite icon;
}
