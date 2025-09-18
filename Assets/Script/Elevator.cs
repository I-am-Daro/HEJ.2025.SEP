using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class Elevator : MonoBehaviour, IInteractable
{
    [Header("Landings (ajtók pozíciója)")]
    [SerializeField] Transform landingA;   // alsó ajtó közép
    [SerializeField] Transform landingB;   // felső ajtó közép

    [Header("Doors (bool param: IsOpen)")]
    [SerializeField] ElevatorDoor doorA;
    [SerializeField] ElevatorDoor doorB;

    [Header("Timing (animok hosszához igazítsd)")]
    [SerializeField] float openTime = 0.35f;   // ajtónyitás klip hossza
    [SerializeField] float closeTime = 0.40f;   // ajtózárás klip hossza
    [SerializeField] float transitTime = 0.20f; // „utazás” (rejtve) idő

    [Header("Snap / FX")]
    [SerializeField] Vector2 playerSnapOffset = Vector2.zero;

    bool busy;

    void Awake()
    {
        // Induláskor mindkét ajtó legyen zárt pózban, animáció lejátszása nélkül.
        if (doorA) doorA.SnapTo(false);
        if (doorB) doorB.SnapTo(false);
    }

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    public string GetPrompt() => busy ? "" : "Use elevator (E)";

    public void Interact(PlayerStats player)
    {
        if (busy || !player) return;
        if (!landingA || !landingB || !doorA || !doorB)
        {
            Debug.LogWarning("[Elevator] Missing refs (landings/doors).");
            return;
        }
        StartCoroutine(Ride(player));
    }

    IEnumerator Ride(PlayerStats player)
    {
        busy = true;

        var rb = player.GetComponent<Rigidbody2D>();
        var pi = player.GetComponent<PlayerInput>();
        var sr = player.GetComponentInChildren<SpriteRenderer>(true);

        // Melyik ajtóhoz vagyunk közelebb?
        Vector3 p = player.transform.position;
        bool fromA = (landingA.position - p).sqrMagnitude <= (landingB.position - p).sqrMagnitude;

        var fromDoor = fromA ? doorA : doorB;
        var toDoor = fromA ? doorB : doorA;
        var toPos = (fromA ? landingB.position : landingA.position) + (Vector3)playerSnapOffset;

        // Input / fizika lock
        if (pi) pi.enabled = false;
        if (rb) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }

        // ---- INDULÓ AJTÓ: NYIT → JÁTÉKOS ELTŰNIK → ZÁR ----
        yield return fromDoor.Open(openTime);

        if (sr) sr.enabled = false;                 // most tűnjön el

        yield return fromDoor.Close(closeTime);     // ajtó csukódik

        // ---- TRANZIT ----
        if (transitTime > 0f) yield return new WaitForSeconds(transitTime);
        player.transform.position = toPos;

        // ---- CÉL AJTÓ: NYIT → JÁTÉKOS MEGJELENIK → ZÁR ----
        yield return toDoor.Open(openTime);

        if (sr) sr.enabled = true;                  // megjelenik

        yield return toDoor.Close(closeTime);

        // Unlock
        if (rb) rb.simulated = true;
        if (pi) pi.enabled = true;

        busy = false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (landingA) Gizmos.DrawWireSphere(landingA.position, 0.15f);
        if (landingB) Gizmos.DrawWireSphere(landingB.position, 0.15f);
        if (landingA && landingB) Gizmos.DrawLine(landingA.position, landingB.position);
    }
#endif
}
