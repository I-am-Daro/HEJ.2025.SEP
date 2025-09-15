using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PowerConsumer : MonoBehaviour
{
    [Tooltip("Ennek az objektumnak a PowerNode-ja (lehet ugyanazon GO-n).")]
    public PowerNode node;

    [Header("Events")]
    public UnityEvent<bool> OnPowerChanged; // UI/anim: true/false

    bool last;

    void Reset()
    {
        if (!node) node = GetComponent<PowerNode>();
        if (!node) node = GetComponentInParent<PowerNode>();
    }

    void OnEnable() { if (!node) Reset(); PowerGrid.I?.Register(this); }
    void OnDisable() { PowerGrid.I?.Unregister(this); }

    public void SetPowered(bool on)
    {
        if (on == last) return;
        last = on;
        OnPowerChanged?.Invoke(on);
    }
}
