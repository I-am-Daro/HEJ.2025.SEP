using UnityEngine;

[DisallowMultipleComponent]
public class PowerNode : MonoBehaviour
{
    [Tooltip("Az ûrhajó (vagy bármely fõforrás) legyen Source.")]
    public bool isSource = false;

    [Tooltip("Igazi csomópont, amin továbbhalad az áram (csõelem).")]
    public bool isConduit = false;

    [Tooltip("Mekkora távolságon belül tekintünk két csomópontot összekötöttnek.")]
    public float linkRadius = 0.6f; // rács=1-hez jó

    [HideInInspector] public bool connectedToSource = false;

    void OnEnable() { PowerGrid.I?.Register(this); }
    void OnDisable() { PowerGrid.I?.Unregister(this); }
}
