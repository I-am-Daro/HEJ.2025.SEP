using UnityEngine;
using UnityEngine.InputSystem;  // Kell, mert InputValue-t használunk

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownMover : MonoBehaviour
{
    [SerializeField] float defaultMoveSpeed = 4f;
    public float MoveSpeed { get; set; }

    Rigidbody2D rb;
    Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        MoveSpeed = defaultMoveSpeed;
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * MoveSpeed;
    }
}
