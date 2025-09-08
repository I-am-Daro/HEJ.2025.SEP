using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MovementZone : MonoBehaviour
{
    public bool changeMode = true;
    public MoveMode modeInZone = MoveMode.ZeroG;

    public bool overrideSpeed = false;
    public float speed = 6f;

    public bool overrideGravity = false;
    public float gravityScale = 0f;

    string playerTag = "Player";

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        var svc = other.GetComponent<PlayerMovementService>();
        if (!svc) return;

        if (changeMode)
            svc.Apply(modeInZone,
                overrideSpeed ? speed : (float?)null,
                overrideGravity ? gravityScale : (float?)null);
        else
        {
            if (overrideSpeed) svc.SetSpeed(speed);
            if (overrideGravity) svc.SetGravity(gravityScale);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        var svc = other.GetComponent<PlayerMovementService>();
        if (!svc) return;

        // visszaállítja a scene-nek megfelelõ default módot
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (scene == "Ship_Interior")
            svc.Apply(MoveMode.InteriorSide);
        else
            svc.Apply(MoveMode.ExteriorTopDown);
    }
}
