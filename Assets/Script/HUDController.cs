using UnityEngine;

public class HUDController : MonoBehaviour
{
    [SerializeField] UIStatusBar o2Bar, energyBar, hungerBar, waterBar;
    [SerializeField] UIStatusBar ironBar;            // <<< �J
    [SerializeField] PlayerStats stats;
    [SerializeField] PlayerInventory inventory;      // <<< �J

    void Awake()
    {
        o2Bar.SetLabel("O2"); o2Bar.SetRange(0, 100);
        energyBar.SetLabel("ENERGY"); energyBar.SetRange(0, 100);
        hungerBar.SetLabel("HUNGER"); hungerBar.SetRange(0, 100);
        waterBar.SetLabel("WATER"); waterBar.SetRange(0, 100);

        if (ironBar)
        {
            ironBar.SetLabel("IRON");
            // vasb�l nincs max 100, de be�ll�thatod dinamikusan is
            ironBar.SetRange(0, 999);
        }
    }

    void Update()
    {
        if (stats)
        {
            o2Bar.SetValue(stats.oxygen, true);
            energyBar.SetValue(stats.energy, true);
            hungerBar.SetValue(stats.hunger, true);
            waterBar.SetValue(stats.water, true);
        }

        if (inventory && ironBar)
        {
            ironBar.SetValue(inventory.ironUnits, true);
        }
    }
}
