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

    // [HideInInspector] public bool connectedToSource = false;
    [SerializeField] public bool connectedToSource = false; // DEBUG: l�tsz�djon

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
