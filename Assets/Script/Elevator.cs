using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Elevator : MonoBehaviour, IInteractable
{
    [Header("Landings (set in inspector)")]
    [SerializeField] Transform landingA;         // Als� ajt� k�z�ppontja
    [SerializeField] Transform landingB;         // Fels� ajt� k�z�ppontja

    [Header("Doors (optional anim)")]
    [SerializeField] Animator doorA;             // Ajt� anim az als� �llom�son (nyit/zar)
    [SerializeField] Animator doorB;             // Ajt� anim a fels� �llom�son
    [SerializeField] float doorAnimTime = 0.35f; // ajt� csuk/nyit id�

    [Header("Snap / feel")]
    [SerializeField] Vector2 playerSnapOffset = new Vector2(0.0f, 0.0f); // hova �ll�tsuk pontosan a j�t�kost
    [SerializeField] float hideDuringTravel = 0.15f; // utaz�skor ennyi id�re elrejtj�k

    [Header("Sorting mask (opcion�lis)")]
    [SerializeField] SpriteRenderer frontDoorOverlay; // egy el�ls� ajt� sprite ami takar (sorting layer: Foreground)

    public string GetPrompt() => "Use elevator (E)";

    public void Interact(PlayerStats player)
    {
        if (!player) return;
        StartCoroutine(Ride(player));
    }

    IEnumerator Ride(PlayerStats player)
    {
        // referenci�k
        var rb = player.GetComponent<Rigidbody2D>();
        var pi = player.GetComponent<PlayerInput>();     // n�lad ez van a Playeren
        var sr = player.GetComponentInChildren<SpriteRenderer>(true);

        // melyik �llom�shoz vagyunk k�zelebb?
        Vector3 p = player.transform.position;
        float dA = (landingA.position - p).sqrMagnitude;
        float dB = (landingB.position - p).sqrMagnitude;
        bool fromA = dA <= dB;                   // innen indulunk
        var fromDoor = fromA ? doorA : doorB;
        var toDoor = fromA ? doorB : doorA;
        var toPos = fromA ? landingB.position : landingA.position;

        // 1) Ajt� csuk + input lock
        if (fromDoor) fromDoor.SetTrigger("Close");
        if (frontDoorOverlay) frontDoorOverlay.enabled = true;

        if (pi) pi.enabled = false;             // teljes kontroll lock
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;               // ne �eszkal�l�djon� a fizika
        }

        yield return new WaitForSeconds(doorAnimTime);

        // 2) L�thatatlann� tessz�k, teleport�lunk
        if (sr) sr.enabled = false;

        // (kis �lm�ny: fekete frame)
        if (hideDuringTravel > 0f)
            yield return new WaitForSeconds(hideDuringTravel);

        player.transform.position = toPos + (Vector3)playerSnapOffset;

        // 3) Fent ajt� nyit
        if (toDoor) toDoor.SetTrigger("Open");
        yield return new WaitForSeconds(doorAnimTime * 0.8f);

        // 4) Vissza�ll�t�s
        if (rb) rb.simulated = true;
        if (pi) pi.enabled = true;
        if (sr) sr.enabled = true;
        if (frontDoorOverlay) frontDoorOverlay.enabled = false;
    }

    // Hasznos gizmo a be�ll�t�shoz
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (landingA) Gizmos.DrawWireSphere(landingA.position, 0.15f);
        if (landingB) Gizmos.DrawWireSphere(landingB.position, 0.15f);
        if (landingA && landingB) Gizmos.DrawLine(landingA.position, landingB.position);
    }
}
