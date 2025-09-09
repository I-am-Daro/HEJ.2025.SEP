using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class SleepPod : MonoBehaviour, IInteractable
{
    [SerializeField] float sleepDurationFade = 0.5f; // kényelmi (ha majd fadelni akarsz)
    public string GetPrompt() => "Sleep";

    public void Interact(PlayerStats player)
    {
        if (!player) return;

        // 1) Töltsd fel az energiát (alvás)
        player.FullRest();

        // 2) +1 nap
        DayNightSystem.Instance?.AdvanceDay();

        // 3) Mentés – checkpoint
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

        Debug.Log("[SleepPod] Slept and saved checkpoint.");
        // Itt késõbb betehetünk fade-et vagy “New Day” feliratot.
    }
}
