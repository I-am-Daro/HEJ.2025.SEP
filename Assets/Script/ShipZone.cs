using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ShipZone : MonoBehaviour
{
    [Header("Recharge Settings")]
    public float o2RechargePerSec = 15f;     // szkafander feltöltés tempója (%/sec)

    [Header("Inventory O₂ usage")]
    public bool consumeOxygenFromInventory = true;
    public float oxygenPerItem = 10f;        // 1 item = ennyi % O₂
    public float ambientOxygenUsePerSecPercent = 1f; // bent ennyit „lélegzünk” / sec (százalékban)

    float _rechargeDebt; // százalék → item levonáshoz
    float _ambientDebt;  // százalék → item levonáshoz

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var stats = other.GetComponent<PlayerStats>();
        if (stats) stats.isInShipInterior = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var stats = other.GetComponent<PlayerStats>();
        if (stats) stats.isInShipInterior = false;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var stats = other.GetComponent<PlayerStats>();
        if (!stats) return;

        var inv = other.GetComponent<PlayerInventory>();

        // 1) szkafander feltöltése (ha nincs tele)
        float missing = 100f - stats.oxygen;
        if (missing > 0.01f)
        {
            float want = Mathf.Min(o2RechargePerSec * Time.deltaTime, missing);

            if (consumeOxygenFromInventory)
            {
                if (!inv || inv.oxygenUnits <= 0 || oxygenPerItem <= 0.0001f) goto AmbientOnly;

                float available = inv.oxygenUnits * oxygenPerItem;
                float used = Mathf.Min(want, available);

                stats.oxygen = Mathf.Min(100f, stats.oxygen + used);

                _rechargeDebt += used;
                int consume = Mathf.FloorToInt(_rechargeDebt / oxygenPerItem);
                if (consume > 0)
                {
                    int toConsume = Mathf.Min(consume, inv.oxygenUnits);
                    inv.oxygenUnits -= toConsume;
                    _rechargeDebt -= toConsume * oxygenPerItem;
                    inv.RaiseChanged();
                }
            }
            else
            {
                stats.oxygen = Mathf.Min(100f, stats.oxygen + want);
            }
        }

    AmbientOnly:
        // 2) bent folyamatosan fogyjon a "házi" O₂ készlet (akkor is, ha a szkafander 100%)
        if (consumeOxygenFromInventory && inv && inv.oxygenUnits > 0 && oxygenPerItem > 0.0001f)
        {
            float ambientUse = Mathf.Max(0f, ambientOxygenUsePerSecPercent) * Time.deltaTime; // %/sec → %
            _ambientDebt += ambientUse;

            int consume = Mathf.FloorToInt(_ambientDebt / oxygenPerItem);
            if (consume > 0)
            {
                int toConsume = Mathf.Min(consume, inv.oxygenUnits);
                inv.oxygenUnits -= toConsume;
                _ambientDebt -= toConsume * oxygenPerItem;
                inv.RaiseChanged();
            }
        }
    }
}
