using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Prompt UI (opcion�lis)")]
    [SerializeField] TextMeshProUGUI promptText;
    [SerializeField] string keyHint = "E";

    PlayerStats stats;
    IInteractable current;
    Collider2D currentCol; // annak az objektumnak a collidere, amiben �pp �llunk

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        HidePrompt();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Csak akkor figyel�nk, ha a m�sik oldalon VAN IInteractable
        var candidate = other.GetComponent<IInteractable>();
        if (candidate == null) return;

        current = candidate;
        currentCol = other;
        ShowPrompt(current.GetPrompt());
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other != currentCol) return;
        current = null;
        currentCol = null;
        HidePrompt();
    }

    // Input System (Send Messages): On + ActionName
    public void OnInteract(InputValue value)
    {
        if (value.Get<float>() <= 0.5f) return;   // csak lenyom�skor
        if (current == null) return;

        current.Interact(stats);
    }

    void ShowPrompt(string text)
    {
        if (!promptText) return;
        promptText.gameObject.SetActive(true);
        promptText.text = $"{text} ({keyHint})";
    }

    void HidePrompt()
    {
        if (!promptText) return;
        promptText.gameObject.SetActive(false);
    }

    public void OnWater(InputValue value)
    {
        if (value.Get<float>() <= 0.5f) return;
        if (current == null) return;

        // Ha a jelenlegi c�l egy BedPlot, h�vd meg rajta a Water()-t
        var bed = current as BedPlot;
        if (bed != null)
        {
            var stats = GetComponent<PlayerStats>();
            bed.Water(stats);
        }
    }
}
