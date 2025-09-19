using UnityEngine;

[DisallowMultipleComponent]
public class WateringCan : MonoBehaviour
{
    [Header("Carry settings")]
    public string handAnchorName = "HandAnchor";
    public Vector3 carryLocalOffset = Vector3.zero;
    public int carrySortingOrderBoost = 10;

    [Header("Optional SFX")]
    public AudioClip pickupSfx;
    public AudioClip putdownSfx;

    // --- state ---
    public bool IsCarried { get; private set; }
    public Transform CarrierAnchor { get; private set; }

    // --- cached ---
    Collider2D col;
    SpriteRenderer[] srs;
    int[] originalOrders;

    // eredeti (scene) állapot
    Transform originalParent;
    Vector3 originalLocalPos;
    Quaternion originalLocalRot;
    Vector3 originalLocalScale;

    // “home” – általában a Station CanAnchor-ja
    Transform homeParent;
    Vector3 homeLocalPos;
    Quaternion homeLocalRot;
    Vector3 homeLocalScale = Vector3.one;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;

        srs = GetComponentsInChildren<SpriteRenderer>(true);
        if (srs != null)
        {
            originalOrders = new int[srs.Length];
            for (int i = 0; i < srs.Length; i++)
                originalOrders[i] = srs[i].sortingOrder;
        }

        originalParent = transform.parent;
        originalLocalPos = transform.localPosition;
        originalLocalRot = transform.localRotation;
        originalLocalScale = transform.localScale;

        // default home a születéskori hely
        homeParent = originalParent;
        homeLocalPos = originalLocalPos;
        homeLocalRot = originalLocalRot;
        homeLocalScale = originalLocalScale;
    }

    // Station hívja a setuphoz
    public void ConfigureHome(Transform parent, Vector3 localPos, Quaternion localRot, Vector3 localScale)
    {
        homeParent = parent != null ? parent : originalParent;
        homeLocalPos = localPos;
        homeLocalRot = localRot;
        homeLocalScale = localScale == Vector3.zero ? Vector3.one : localScale;
    }

    Transform FindOrMakeHandAnchor(Transform root, string name)
    {
        var t = root.Find(name);
        if (t) return t;
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go.transform;
    }

    // ---- publikus API ----
    public void PickUp(PlayerStats player, PlayerInventory inv)
    {
        if (IsCarried || player == null || inv == null) return;
        if (!inv.TryTakeCan(this)) return; // ha már fog valamit, false

        CarrierAnchor = FindOrMakeHandAnchor(player.transform, handAnchorName);
        CarrierAnchor.localScale = Vector3.one; // ne torzítson

        transform.SetParent(CarrierAnchor, false);
        transform.localPosition = carryLocalOffset;
        transform.localRotation = Quaternion.identity;
        transform.localScale = originalLocalScale;

        if (col) col.enabled = false;

        if (srs != null)
            for (int i = 0; i < srs.Length; i++)
                srs[i].sortingOrder = originalOrders[i] + carrySortingOrderBoost;

        if (pickupSfx) AudioSource.PlayClipAtPoint(pickupSfx, player.transform.position);

        IsCarried = true;
    }

    public void PutBackHome()
    {
        // visszaállítás: identitás scale-ű anchor alá
        transform.SetParent(homeParent, false);
        transform.localPosition = homeLocalPos;
        transform.localRotation = homeLocalRot;
        transform.localScale = homeLocalScale;

        if (col) col.enabled = true;

        if (srs != null)
            for (int i = 0; i < srs.Length; i++)
                srs[i].sortingOrder = originalOrders[i];

        if (putdownSfx) AudioSource.PlayClipAtPoint(putdownSfx, transform.position);

        CarrierAnchor = null;
        IsCarried = false;
    }
}
