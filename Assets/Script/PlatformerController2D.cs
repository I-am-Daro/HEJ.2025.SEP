using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlatformerController2D : MonoBehaviour
{
    [SerializeField] float defaultMoveSpeed = 5f;
    public float MoveSpeed { get; set; }

    Rigidbody2D rb;
    float moveX;
    PlayerStats stats;   // <-- �J

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        MoveSpeed = defaultMoveSpeed;
        stats = GetComponent<PlayerStats>(); // <-- �J
    }

    public void OnMove(InputValue value)
    {
        Vector2 v = value.Get<Vector2>();
        moveX = v.x;
    }

    void FixedUpdate()
    {
        float mult = (stats != null) ? stats.MoveSpeedMultiplier : 1f; // <-- �hs�g lass�t
        rb.linearVelocity = new Vector2(moveX * (MoveSpeed * mult), rb.linearVelocity.y);
    }
}
