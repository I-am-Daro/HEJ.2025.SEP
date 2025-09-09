public interface IInteractable
{
    /// <summary>UI-hoz: mit �rjunk ki (pl. "Eat").</summary>
    string GetPrompt();

    /// <summary>A Player interakci�kor ezt h�vja.</summary>
    void Interact(PlayerStats player);
}