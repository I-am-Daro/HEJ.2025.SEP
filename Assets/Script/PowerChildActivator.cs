using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PowerNode))]
public class PowerChildActivator : MonoBehaviour
{
    [Header("What to toggle on connection")]
    [SerializeField] GameObject childToActivate;

    [Tooltip("Kapcsolja ki a child-ot, ha megsz�nik a h�l�zat?")]
    [SerializeField] bool deactivateWhenDisconnected = true;

    [Tooltip("Ha nincs k�zzel be�ll�tva, megpr�b�lja n�v alapj�n megtal�lni a child-ot.")]
    [SerializeField] bool autoFindByName = true;

    [SerializeField] string childName = "GravityEmitter";

    PowerNode node;
    bool lastState;

    bool IsGhost() => GetComponentInParent<GhostMarker>() != null;

    void Awake()
    {
        node = GetComponent<PowerNode>();

        if (!childToActivate && autoFindByName && !string.IsNullOrEmpty(childName))
        {
            var t = transform.Find(childName);
            if (t) childToActivate = t.gameObject;
        }
    }

    void OnEnable()
    {
        // Ghost p�ld�nyon nem csin�lunk semmit (BuildManager �gyis letilt minden MonoBehaviour-t)
        if (IsGhost()) return;
        Apply(node && node.connectedToSource);
    }

    void Update()
    {
        if (IsGhost() || !node || !childToActivate) return;

        if (node.connectedToSource != lastState)
            Apply(node.connectedToSource);
    }

    void Apply(bool isConnected)
    {
        lastState = isConnected;

        if (!childToActivate) return;

        bool targetActive = isConnected || !deactivateWhenDisconnected;
        if (childToActivate.activeSelf != targetActive)
            childToActivate.SetActive(targetActive);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!node) node = GetComponent<PowerNode>();
        if (!childToActivate && autoFindByName && !string.IsNullOrEmpty(childName))
        {
            var t = transform.Find(childName);
            if (t) childToActivate = t.gameObject;
        }
    }
#endif
}
