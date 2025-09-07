using UnityEngine;
using UnityEngine.InputSystem;  // Kell, mert InputValue-t használunk

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownMover : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    private Rigidbody2D rb;
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // FONTOS: a metódus neve pontosan On + ActionName
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        UnityEngine.Debug.Log("Move Input: " + moveInput);
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }
}
