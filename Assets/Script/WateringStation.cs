using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WateringStation : MonoBehaviour, IInteractable
{
    [Header("Refs")]
    public WateringCan can;

    [Header("Anchor (scale=1,1,1!)")]
    public Transform placeAnchor;                 // IDE tegy�k vissza a kann�t
    public Vector3 canLocalPosition = Vector3.zero;
    public Vector3 canLocalEulerAngles = Vector3.zero;
    public Vector3 canLocalScale = Vector3.one;

    [Header("Prompts")]
    public string promptWhenEmpty = "This is the water station";
    public string promptWhenCarrying = "Put back watering can (E)";

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // Ha nincs anchor be�ll�tva, hozzunk l�tre egyet UNIT sk�l�val,
        // hogy a kann�t SOHA ne torz�tsa a parent sk�l�ja.
        if (placeAnchor == null)
        {
            var anchor = new GameObject("CanAnchor");
            placeAnchor = anchor.transform;
            placeAnchor.SetParent(transform, false);
            placeAnchor.localPosition = Vector3.zero;
            placeAnchor.localRotation = Quaternion.identity;
            placeAnchor.localScale = Vector3.one;
        }
    }

    void Start()
    {
        // �ll�tsuk be a kanna �home�-j�t az anchorra
        if (can != null)
        {
            var rot = Quaternion.Euler(canLocalEulerAngles);
            can.ConfigureHome(placeAnchor, canLocalPosition, rot, canLocalScale);
            // indul�skor biztos ami biztos: tegy�k a hely�re a kann�t
            can.PutBackHome();
        }
        else
        {
            Debug.LogWarning("[WateringStation] 'can' reference is null.");
        }
    }

    public string GetPrompt()
    {
        var player = FindAnyObjectByType<PlayerStats>();
        var inv = player ? player.GetComponent<PlayerInventory>() : null;
        if (inv && inv.HasWateringCan) return promptWhenCarrying;
        return promptWhenEmpty;
    }

    public void Interact(PlayerStats player)
    {
        if (!player) return;
        var inv = player.GetComponent<PlayerInventory>();
        if (!inv) return;

        if (inv.HasWateringCan && inv.CurrentCan != null)
        {
            var c = inv.CurrentCan;
            if (!inv.TryReturnCan(c)) return;

            c.PutBackHome();  // <<< SK�L�Z�S-SP�ILER: sosem torzul, mert az anchor scale=1
        }
    }
}
