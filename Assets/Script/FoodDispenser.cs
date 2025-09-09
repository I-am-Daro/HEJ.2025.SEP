using System.Diagnostics;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FoodDispenser : MonoBehaviour, IInteractable
{
    [SerializeField] float hungerDecrease = 35f;
    [SerializeField] float cooldown = 2f;
    float readyAt = 0f;

    void Reset()
    {
        // kényelmi: legyen trigger, hogy a Player közelítését érzékeljük
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    public string GetPrompt() => "Eat";

    public void Interact(PlayerStats player)
    {
        if (!player) return;
        if (Time.time < readyAt) return;

        player.Eat(hungerDecrease);
        readyAt = Time.time + cooldown;

        // TODO: itt lehet SFX/anim, pl. AudioSource.PlayClipAtPoint(clip, transform.position);
    }
}
