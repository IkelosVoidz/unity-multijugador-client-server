using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//S'haura de canviar tot aquest script per al treball pero de moment funciona

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] Rigidbody2D rb;
    [SerializeField] ClientPlayerBehaviour playerBehaviour;
    [SerializeField] float moveSpeed = 5f;


    private void OnEnable() { ClientBehaviour.OnSelfMoved += FixPosition; }

    private void OnDisable() { ClientBehaviour.OnSelfMoved -= FixPosition; }

    void FixedUpdate()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        Vector2 moveDirection = new Vector2(moveX, moveY).normalized;
        rb.velocity = new Vector2(moveDirection.x * moveSpeed, rb.velocity.y);

        playerBehaviour.UpdateServerPosition(rb.position);
    }

    private void FixPosition(Vector2 position)
    {
        rb.velocity = Vector2.zero;
        rb.position = position;
    }
}
