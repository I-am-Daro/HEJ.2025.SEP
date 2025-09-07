using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class SideScrollerNoJump : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    Rigidbody2D rb;
    float moveX;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    public void OnMove(InputAction.CallbackContext ctx)
    {
        var v = ctx.ReadValue<Vector2>();
        moveX = v.x;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);
    }
}
