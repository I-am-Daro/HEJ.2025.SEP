using UnityEngine;
using TMPro;

public class HUDInventoryView : MonoBehaviour
{
    [SerializeField] PlayerInventory inventory;        // Playeren lévõ
    [SerializeField] TextMeshProUGUI oxygenText;       // "O2: 12"
    [SerializeField] TextMeshProUGUI foodText;         // "Food: 7"
    [SerializeField] TextMeshProUGUI ironText;         // <<< ÚJ: "Iron: 15"

    void OnEnable()
    {
        if (!inventory) inventory = FindFirstObjectByType<PlayerInventory>();
        if (inventory != null)
        {
            inventory.OnChanged += UpdateView;
            UpdateView();
        }
    }
    void OnDisable()
    {
        if (inventory != null) inventory.OnChanged -= UpdateView;
    }

    void UpdateView()
    {
        if (!inventory) return;
        if (oxygenText) oxygenText.text = $"O2  {inventory.oxygenUnits}";
        if (foodText) foodText.text = $"Food  {inventory.foodUnits}";
        if (ironText) ironText.text = $"Iron  {inventory.ironUnits}";
    }
}
