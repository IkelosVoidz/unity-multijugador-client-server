using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerPlayerBehaviour : PlayerBehaviour
{
    private Vector2 currentVelocity;
    private Vector2 targetPosition;
    [SerializeField] private float lerpSpeed = 5f;

    private float dir = 1.0f;

    private void Start()
    {
        currentVelocity = transform.position;
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

    private void UpdatePositionFromServer(string character, Vector2 position)
    {
        if (characterName != character) return;
        targetPosition = position;
    }

    public override void ActivateAbility(float dir)
    {
        base.ActivateAbility(dir);
        this.dir = dir;
    }


    private new void Update()
    {
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        dir = direction.x;

        sr.flipX = dir < 0f;

        Vector2 desiredVelocity = direction * 5;
        currentVelocity = Vector2.Lerp(currentVelocity, desiredVelocity, Time.deltaTime * lerpSpeed);
        rb.velocity = currentVelocity;

        base.Update();
        // currentPosition = Vector2.Lerp(currentPosition, targetPosition, Time.deltaTime * lerpSpeed);
        // transform.position = currentPosition;
    }
}
