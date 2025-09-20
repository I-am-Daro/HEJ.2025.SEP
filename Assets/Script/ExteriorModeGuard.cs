using UnityEngine;

[DisallowMultipleComponent]
public class ExteriorModeGuard : MonoBehaviour
{
    [SerializeField] bool logCorrections = false;

    ZeroGOverlapTracker tracker;
    PlayerMovementService move;
    PlayerStats stats;

    void Awake()
    {
        tracker = GetComponent<ZeroGOverlapTracker>();
        move = GetComponent<PlayerMovementService>();
        stats = GetComponent<PlayerStats>();
    }

    void LateUpdate()
    {
        if (!move) return;

        // Csak exterior mozgásmódoknál õrködünk
        if (move.CurrentMode != MoveMode.ExteriorTopDown &&
            move.CurrentMode != MoveMode.ZeroG)
            return;

        bool shouldZeroG = tracker ? tracker.IsZeroG : true; // ha nincs tracker, inkább ZeroG
        bool isZeroGMode = (move.CurrentMode == MoveMode.ZeroG);

        if (shouldZeroG != isZeroGMode)
        {
            if (stats) stats.isZeroG = shouldZeroG;
            move.Apply(shouldZeroG ? MoveMode.ZeroG : MoveMode.ExteriorTopDown);
            if (logCorrections)
                Debug.LogWarning($"[ExteriorModeGuard] Corrected movement to {(shouldZeroG ? "ZeroG" : "TopDown")}");
        }
    }
}
