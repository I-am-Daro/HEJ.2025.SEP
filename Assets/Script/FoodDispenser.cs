using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FoodDispenser : MonoBehaviour, IInteractable
{
    [Header("Use Settings")]
    public int foodUnitsPerUse = 1;   // ennyit eszik a hûtõbõl
    public float nutritionPerUnit = 25f; // 1 egység mennyit csökkent a hungerbõl (%-ban)

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    public string GetPrompt() => "Eat (E)";

    public void Interact(PlayerStats player)
    {
        if (!player) return;
        var inv = player.GetComponent<PlayerInventory>();
        if (!inv)
        {
            UnityEngine.Debug.LogWarning("[FoodDispenser] PlayerInventory missing.");
            return;
        }

        if (inv.foodUnits < foodUnitsPerUse)
        {
            UnityEngine.Debug.Log("[FoodDispenser] No food.");
            return;
        }

        // fogyasztás
        inv.foodUnits -= foodUnitsPerUse;
        inv.RaiseChanged();

        // hatás
        player.Eat(nutritionPerUnit * foodUnitsPerUse);
        UnityEngine.Debug.Log($"[FoodDispenser] Ate {foodUnitsPerUse}, hunger now ~{player.hunger:0}.");
    }
}
