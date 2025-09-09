public interface IInteractable
{
    /// <summary>UI-hoz: mit írjunk ki (pl. "Eat").</summary>
    string GetPrompt();

    /// <summary>A Player interakciókor ezt hívja.</summary>
    void Interact(PlayerStats player);
}