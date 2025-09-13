using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class SleepPod : MonoBehaviour, IInteractable
{
    [Header("Sleep Effects")]
    [Tooltip("Hány %-ot nőjön a Hunger alváskor.")]
    [SerializeField] float hungerIncreaseOnSleep = 10f;

    [Tooltip("Hány darab O2 item fogyjon el alvás alatt az inventoryból.")]
    [SerializeField] int oxygenItemsConsumedOnSleep = 1;

    [Header("FX (optional)")]
    [SerializeField] float sleepDurationFade = 0.5f; // ha majd fadelni akarsz

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    public string GetPrompt() => "Sleep";

    public void Interact(PlayerStats player)
    {
        if (!player) return;

        // 1) Energia feltöltése (alvás)
        player.FullRest();

        // 2) Éhség nő (idő telik)
        player.hunger = Mathf.Clamp(player.hunger + Mathf.Max(0f, hungerIncreaseOnSleep), 0f, 100f);

        // 3) O2 készlet fogy az inventoryból (alvás alatti "lélegzés")
        var inv = player.GetComponent<PlayerInventory>();
        if (inv && oxygenItemsConsumedOnSleep > 0 && inv.oxygenUnits > 0)
        {
            int take = Mathf.Min(oxygenItemsConsumedOnSleep, inv.oxygenUnits);
            inv.oxygenUnits -= take;
            inv.RaiseChanged();
        }

        // 4) +1 nap (ez triggereli a növények növekedését is)
        DayNightSystem.Instance?.AdvanceDay();

        // 5) Mentés – checkpoint az alvás utáni állapotról
        var save = new GameSave
        {
            day = DayNightSystem.Instance ? DayNightSystem.Instance.CurrentDay : 1,
            sceneName = SceneManager.GetActiveScene().name,
            playerPosition = player.transform.position,
            oxygen = player.oxygen,
            energy = player.energy,
            hunger = player.hunger
        };
        SaveSystem.Save(save);

        UnityEngine.Debug.Log("[SleepPod] Slept: energy full, hunger increased, O2 items consumed, checkpoint saved.");
        // ide jöhet később fade / "New Day" UI
    }
}
