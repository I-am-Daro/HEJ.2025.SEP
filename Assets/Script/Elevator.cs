using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Elevator : MonoBehaviour, IInteractable
{
    [Header("Landings (set in inspector)")]
    [SerializeField] Transform landingA;         // Alsó ajtó középpontja
    [SerializeField] Transform landingB;         // Felsõ ajtó középpontja

    [Header("Doors (optional anim)")]
    [SerializeField] Animator doorA;             // Ajtó anim az alsó állomáson (nyit/zar)
    [SerializeField] Animator doorB;             // Ajtó anim a felsõ állomáson
    [SerializeField] float doorAnimTime = 0.35f; // ajtó csuk/nyit idõ

    [Header("Snap / feel")]
    [SerializeField] Vector2 playerSnapOffset = new Vector2(0.0f, 0.0f); // hova állítsuk pontosan a játékost
    [SerializeField] float hideDuringTravel = 0.15f; // utazáskor ennyi idõre elrejtjük

    [Header("Sorting mask (opcionális)")]
    [SerializeField] SpriteRenderer frontDoorOverlay; // egy elülsõ ajtó sprite ami takar (sorting layer: Foreground)

    public string GetPrompt() => "Use elevator (E)";

    public void Interact(PlayerStats player)
    {
        if (!player) return;
        StartCoroutine(Ride(player));
    }

    IEnumerator Ride(PlayerStats player)
    {
        // referenciák
        var rb = player.GetComponent<Rigidbody2D>();
        var pi = player.GetComponent<PlayerInput>();     // nálad ez van a Playeren
        var sr = player.GetComponentInChildren<SpriteRenderer>(true);

        // melyik állomáshoz vagyunk közelebb?
        Vector3 p = player.transform.position;
        float dA = (landingA.position - p).sqrMagnitude;
        float dB = (landingB.position - p).sqrMagnitude;
        bool fromA = dA <= dB;                   // innen indulunk
        var fromDoor = fromA ? doorA : doorB;
        var toDoor = fromA ? doorB : doorA;
        var toPos = fromA ? landingB.position : landingA.position;

        // 1) Ajtó csuk + input lock
        if (fromDoor) fromDoor.SetTrigger("Close");
        if (frontDoorOverlay) frontDoorOverlay.enabled = true;

        if (pi) pi.enabled = false;             // teljes kontroll lock
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;               // ne “eszkalálódjon” a fizika
        }

        yield return new WaitForSeconds(doorAnimTime);

        // 2) Láthatatlanná tesszük, teleportálunk
        if (sr) sr.enabled = false;

        // (kis élmény: fekete frame)
        if (hideDuringTravel > 0f)
            yield return new WaitForSeconds(hideDuringTravel);

        player.transform.position = toPos + (Vector3)playerSnapOffset;

        // 3) Fent ajtó nyit
        if (toDoor) toDoor.SetTrigger("Open");
        yield return new WaitForSeconds(doorAnimTime * 0.8f);

        // 4) Visszaállítás
        if (rb) rb.simulated = true;
        if (pi) pi.enabled = true;
        if (sr) sr.enabled = true;
        if (frontDoorOverlay) frontDoorOverlay.enabled = false;
    }

    // Hasznos gizmo a beállításhoz
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (landingA) Gizmos.DrawWireSphere(landingA.position, 0.15f);
        if (landingB) Gizmos.DrawWireSphere(landingB.position, 0.15f);
        if (landingA && landingB) Gizmos.DrawLine(landingA.position, landingB.position);
    }
}
