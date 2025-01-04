using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerPlayerBehaviour : PlayerBehaviour
{
    private Vector2 currentVelocity;
    private Vector2 targetPosition;
    private Vector2 targetVelocity;



    [SerializeField] private float lerpSpeed = 5f;

    private float dir = 1.0f;

    private void Start()
    {
        targetVelocity = rb.velocity;
        targetPosition = transform.position;
    }

    private void OnEnable()
    {
        ClientBehaviour.OnOtherCharacterMoved += UpdatePositionFromServer;
        ClientBehaviour.OnOtherCharacterAbilityActivated += ActivateAbility;
    }
    private void OnDisable()
    {
        ClientBehaviour.OnOtherCharacterMoved -= UpdatePositionFromServer;
        ClientBehaviour.OnOtherCharacterAbilityActivated -= ActivateAbility;

    }

    private void UpdatePositionFromServer(string character, Vector2 position, Vector2 velocity)
    {
        if (characterName != character) return;
        targetPosition = position;
        targetVelocity = velocity;
    }

    public override void ActivateAbility(Vector2 position, float dir)
    {
        base.ActivateAbility(position, dir);
        this.dir = dir;
        targetPosition = position;
    }


    private new void Update()
    {
        Vector2 currentPosition = transform.position;

        if (targetVelocity.x != 0) dir = targetVelocity.x;

        sr.flipX = dir < 0.0f;
        animator.SetFloat("Speed", targetVelocity.magnitude);
        currentPosition = Vector2.Lerp(currentPosition, targetPosition, Time.deltaTime * lerpSpeed);
        transform.position = currentPosition;
    }
}
