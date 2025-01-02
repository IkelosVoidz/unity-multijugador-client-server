using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class PlayerMovement : MonoBehaviour
{
    [SerializeField] Rigidbody2D rb;
    [SerializeField] ClientPlayerBehaviour playerBehaviour;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float jumpForce = 10f;
    [SerializeField] LayerMask groundLayer;

    private float dir = 1.0f;
    private bool isGrounded = false;

    private void OnEnable() { ClientBehaviour.OnSelfMoved += FixPosition; }

    private void OnDisable() { ClientBehaviour.OnSelfMoved -= FixPosition; }

    private void Update()
    {
        playerBehaviour.dir = dir; //no me hableis de esto, me da mucha pereza hacerlo bien
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            isGrounded = false;
        }
    }

    void FixedUpdate()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        Vector2 moveDirection = new Vector2(moveX, moveY).normalized;
        rb.velocity = new Vector2(moveDirection.x * moveSpeed, rb.velocity.y);

        if (moveX != 0) dir = moveX;
        playerBehaviour.UpdateServerPosition(rb.position);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            isGrounded = false;
        }
    }

    private void FixPosition(Vector2 position)
    {
        rb.velocity = Vector2.zero;
        rb.position = position;
    }
}
