using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    IInteractable current;

    void OnTriggerEnter2D(Collider2D other)
    {
        current = other.GetComponent<IInteractable>();
    }
    void OnTriggerExit2D(Collider2D other)
    {
        if (current != null && other.GetComponent<IInteractable>() == current)
            current = null;
    }

    public void OnInteract(InputValue v)
    {
        if (v.Get<float>() <= 0.5f || current == null) return;
        current.Interact(GetComponent<PlayerStats>());
    }

    // ÚJ: Water action (Keyboard F, Gamepad shoulder)
    public void OnWater(InputValue v)
    {
        if (v.Get<float>() <= 0.5f || current == null) return;

        var bed = current as BedPlot;
        if (bed != null)
            bed.Water(GetComponent<PlayerStats>());
    }
}
