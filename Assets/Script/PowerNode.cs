using UnityEngine;

[DisallowMultipleComponent]
public class PowerNode : MonoBehaviour
{
    [Tooltip("Az ûrhajó (vagy bármely fõforrás) legyen Source.")]
    public bool isSource = false;

    [Tooltip("Igazi csomópont, amin továbbhalad az áram (csõelem vagy épület, ami átvezeti).")]
    public bool isConduit = false;

    [Tooltip("Mekkora távolságon belül tekintünk két csomópontot összekötöttnek.")]
    public float linkRadius = 0.6f; // rács=1-hez jó

    [SerializeField] public bool connectedToSource = false; // debug view

    bool IsGhost() => GetComponentInParent<GhostMarker>() != null;

    void OnEnable()
    {
        if (IsGhost()) return;             // GHOST példányt kihagyjuk
        PowerGrid.I?.Register(this);
    }

    void OnDisable()
    {
        if (IsGhost()) return;             // GHOST-nál semmi dolgunk
        PowerGrid.I?.Unregister(this);     // rendes leiratkozás
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = connectedToSource ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, linkRadius);
    }
#endif
}
