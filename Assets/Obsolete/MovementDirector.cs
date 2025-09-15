using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(10000)]
public class MovementDirector : MonoBehaviour
{
    public static MovementDirector I { get; private set; }

    PlayerMovementService mover;
    PlayerStats stats;
    MovementMarker marker;

    MoveMode lastApplied;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (I == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // Player / marker újrakeresés
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            mover = player.GetComponent<PlayerMovementService>();
            stats = player.GetComponent<PlayerStats>();
        }
        else { mover = null; stats = null; }

        marker = FindAnyObjectByType<MovementMarker>();

        ApplyForScene(force: true);
    }

    void LateUpdate()
    {
        // Exteriorban a ZeroG flag dinamikus – figyeljük
        ApplyForScene(force: false);
    }

    void ApplyForScene(bool force)
    {
        if (!mover || marker == null) return;

        MoveMode desired = ComputeDesiredMode();
        if (force || desired != lastApplied)
        {
            mover.Apply(desired);
            lastApplied = desired;
        }
    }

    MoveMode ComputeDesiredMode()
    {
        if (marker.Kind == SceneMovementKind.Interior)
            return MoveMode.InteriorSide;

        // Exterior: ZeroG vagy TopDown
        bool zero = stats && stats.isZeroG;
        return zero ? MoveMode.ZeroG : MoveMode.ExteriorTopDown;
    }
}
