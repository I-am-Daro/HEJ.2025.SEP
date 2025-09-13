using UnityEngine;

[DisallowMultipleComponent]
public class PowerNode : MonoBehaviour
{
    [Tooltip("Az �rhaj� (vagy b�rmely f�forr�s) legyen Source.")]
    public bool isSource = false;

    [Tooltip("Igazi csom�pont, amin tov�bbhalad az �ram (cs�elem).")]
    public bool isConduit = false;

    [Tooltip("Mekkora t�vols�gon bel�l tekint�nk k�t csom�pontot �sszek�t�ttnek.")]
    public float linkRadius = 0.6f; // r�cs=1-hez j�

    [HideInInspector] public bool connectedToSource = false;

    void OnEnable() { PowerGrid.I?.Register(this); }
    void OnDisable() { PowerGrid.I?.Unregister(this); }
}
