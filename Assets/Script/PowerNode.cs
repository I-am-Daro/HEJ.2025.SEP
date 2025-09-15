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

    // [HideInInspector] public bool connectedToSource = false;
    [SerializeField] public bool connectedToSource = false; // DEBUG: látszódjon

    void OnEnable()
    {
        PowerGrid.I?.Register(this);
    }
    void OnDisable()
    {
        Debug.Log($"[PowerNode] UNREGISTER name='{name}'");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = connectedToSource ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, linkRadius);
    }
#endif
}
