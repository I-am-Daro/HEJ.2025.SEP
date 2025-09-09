using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class SleepPod : MonoBehaviour, IInteractable
{
    [SerializeField] float sleepDurationFade = 0.5f; // k�nyelmi (ha majd fadelni akarsz)
    public string GetPrompt() => "Sleep";

    public void Interact(PlayerStats player)
    {
        if (!player) return;

        // 1) T�ltsd fel az energi�t (alv�s)
        player.FullRest();

        // 2) +1 nap
        DayNightSystem.Instance?.AdvanceDay();

        // 3) Ment�s � checkpoint
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
        // Itt k�s�bb betehet�nk fade-et vagy �New Day� feliratot.
    }
}
