using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WateringStation : MonoBehaviour, IInteractable
{
    [Header("Refs")]
    public WateringCan can;

    [Header("Anchor (scale = 1,1,1!)")]
    public Transform placeAnchor;                 // IDE tesszük vissza a kannát
    public Vector3 canLocalPosition = Vector3.zero;
    public Vector3 canLocalEulerAngles = Vector3.zero;
    public Vector3 canLocalScale = Vector3.one;

    [Header("Prompts")]
    public string promptWhenEmpty = "Take watering can (E)";
    public string promptWhenCarrying = "Put back watering can (E)";

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // ha nincs anchor, hozzunk létre unit-scale anchor-t
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
        if (can != null)
        {
            var rot = Quaternion.Euler(canLocalEulerAngles);
            can.ConfigureHome(placeAnchor, canLocalPosition, rot, canLocalScale);
            can.PutBackHome(); // induláskor a helyére tesszük
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

        if (inv != null && inv.HasWateringCan) return promptWhenCarrying;
        return promptWhenEmpty;
    }

    public void Interact(PlayerStats player)
    {
        if (!player || can == null) return;

        var inv = player.GetComponent<PlayerInventory>();
        if (!inv) return;

        // Ha nála van KANNÁJA → tegyük vissza (csak a SAJÁT kannánkat fogadjuk vissza)
        if (inv.HasWateringCan)
        {
            var carried = inv.CurrentCan;
            if (carried == null) return;

            // Ha mást visz (nem ennek a stationnek a kannáját), akkor is visszarakjuk ezt a station ‘can’-jét?
            // Döntés: kizárólag a SAJÁT can-t fogadjuk vissza:
            if (carried != can) return;

            if (!inv.TryReturnCan(can)) return;
            can.PutBackHome();
            return;
        }

        // Ha nincs nála → vegye fel EBBŐL az állomásból
        // (Csak akkor engedjük, ha a can tényleg nálunk van és nincs épp kézben)
        if (!can.IsCarried)
        {
            can.PickUp(player, inv);
        }
    }
}
