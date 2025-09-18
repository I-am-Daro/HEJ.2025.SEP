using UnityEngine;

[DisallowMultipleComponent]
public class PowerNode : MonoBehaviour
{
    [Tooltip("Az �rhaj� (vagy b�rmely f�forr�s) legyen Source.")]
    public bool isSource = false;

    [Tooltip("Igazi csom�pont, amin tov�bbhalad az �ram (cs�elem vagy �p�let, ami �tvezeti).")]
    public bool isConduit = false;

    [Tooltip("Mekkora t�vols�gon bel�l tekint�nk k�t csom�pontot �sszek�t�ttnek.")]
    public float linkRadius = 0.6f; // r�cs=1-hez j�

    [SerializeField] public bool connectedToSource = false; // debug view

    bool IsGhost() => GetComponentInParent<GhostMarker>() != null;

    void OnEnable()
    {
        if (IsGhost()) return;             // GHOST p�ld�nyt kihagyjuk
        PowerGrid.I?.Register(this);
    }

    void OnDisable()
    {
        if (IsGhost()) return;             // GHOST-n�l semmi dolgunk
        PowerGrid.I?.Unregister(this);     // rendes leiratkoz�s
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = connectedToSource ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, linkRadius);
    }
#endif
}
