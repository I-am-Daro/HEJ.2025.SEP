using UnityEngine;

public class ExteriorModeGuard : MonoBehaviour
{
    void LateUpdate()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (!p) return;

        var svc = p.GetComponent<PlayerMovementService>();
        var stats = p.GetComponent<PlayerStats>();
        if (!svc) return;

        var desired = (stats && stats.isZeroG) ? MoveMode.ZeroG : MoveMode.ExteriorTopDown;
        if (svc.CurrentMode != desired)
        {
            svc.Apply(desired);
            Debug.LogWarning($"[ExteriorModeGuard] Corrected movement back to {desired}.");
        }
    }
}
