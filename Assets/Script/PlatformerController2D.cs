using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlatformerController2D : MonoBehaviour
{
    [SerializeField] float defaultMoveSpeed = 5f;
    public float MoveSpeed { get; set; }

    Rigidbody2D rb;
    float moveX;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        MoveSpeed = defaultMoveSpeed;
    }

    public void OnMove(InputValue value)
    {
        Vector2 v = value.Get<Vector2>();
        moveX = v.x;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveX * MoveSpeed, rb.linearVelocity.y);
    }
}
