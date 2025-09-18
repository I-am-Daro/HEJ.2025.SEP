using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WateringCan : MonoBehaviour, IInteractable
{
    [Header("Carry settings")]
    public string handAnchorName = "HandAnchor";
    public Vector3 carryLocalOffset = Vector3.zero;
    public int carrySortingOrderBoost = 10;

    [Header("Optional SFX")]
    public AudioClip pickupSfx;
    public AudioClip putdownSfx;

    // --- cached ---
    Collider2D col;
    SpriteRenderer[] srs;
    int[] originalOrders;

    // EREDETI (scene-ben lévő) állapot (ha nem adsz külön home anchor-t, ide rakjuk vissza)
    Transform originalParent;
    Vector3 originalLocalPos;
    Quaternion originalLocalRot;
    Vector3 originalLocalScale;

    // „Home” (ahová visszakerül) – általában a Station „CanAnchor”-ja
    Transform homeParent;
    Vector3 homeLocalPos;
    Quaternion homeLocalRot;
    Vector3 homeLocalScale = Vector3.one;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;

        srs = GetComponentsInChildren<SpriteRenderer>(true);
        if (srs != null)
        {
            originalOrders = new int[srs.Length];
            for (int i = 0; i < srs.Length; i++) originalOrders[i] = srs[i].sortingOrder;
        }

        originalParent = transform.parent;
        originalLocalPos = transform.localPosition;
        originalLocalRot = transform.localRotation;
        originalLocalScale = transform.localScale;

        // Alapértelmezett „home”: a születéskori hely
        homeParent = originalParent;
        homeLocalPos = originalLocalPos;
        homeLocalRot = originalLocalRot;
        homeLocalScale = originalLocalScale;
    }

    // Station hívja beállításkor (ajánlott a Station CanAnchor-ját átadni)
    public void ConfigureHome(Transform parent, Vector3 localPos, Quaternion localRot, Vector3 localScale)
    {
        homeParent = parent != null ? parent : originalParent;
        homeLocalPos = localPos;
        homeLocalRot = localRot;
        homeLocalScale = localScale == Vector3.zero ? Vector3.one : localScale;
    }

    public string GetPrompt() => "Pick up watering can (E)";

    public void Interact(PlayerStats player)
    {
        if (!player) return;
        var inv = player.GetComponent<PlayerInventory>();
        if (!inv) return;
        if (!inv.TryTakeCan(this)) return; // már van nála vagy nem ok

        // kézbe tesszük
        Transform anchor = FindOrMakeHandAnchor(player.transform, handAnchorName);
        // FONTOS: a hand anchor-nak legyen unit scale, hogy ne torzítson
        anchor.localScale = Vector3.one;

        transform.SetParent(anchor, false);
        transform.localPosition = carryLocalOffset;
        transform.localRotation = Quaternion.identity;
        transform.localScale = originalLocalScale; // tartsuk az eredeti skálát kézben is

        if (col) col.enabled = false;

        if (srs != null)
            for (int i = 0; i < srs.Length; i++)
                srs[i].sortingOrder = originalOrders[i] + carrySortingOrderBoost;

        if (pickupSfx) AudioSource.PlayClipAtPoint(pickupSfx, player.transform.position);
    }

    public void PutBackHome()
    {
        // VISSZAÁLLÍTÁS: identitás-skálájú („CanAnchor”) szülő alá tesszük
        transform.SetParent(homeParent, false); // false → lokális értékeket közvetlen állítjuk
        transform.localPosition = homeLocalPos;
        transform.localRotation = homeLocalRot;
        transform.localScale = homeLocalScale;

        if (col) col.enabled = true;

        if (srs != null)
            for (int i = 0; i < srs.Length; i++)
                srs[i].sortingOrder = originalOrders[i];

        if (putdownSfx) AudioSource.PlayClipAtPoint(putdownSfx, transform.position);
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
}
