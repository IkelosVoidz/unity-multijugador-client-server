using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerPlayerBehaviour : PlayerBehaviour
{
    private Vector2 currentVelocity;
    private Vector2 targetPosition;
    [SerializeField] private float lerpSpeed = 5f;

    private void Start()
    {
        currentVelocity = transform.position;
        targetPosition = transform.position;
    }

    private void OnEnable() { ClientBehaviour.OnOtherCharacterMoved += UpdatePositionFromServer; }
    private void OnDisable() { ClientBehaviour.OnOtherCharacterMoved -= UpdatePositionFromServer; }

    private void UpdatePositionFromServer(string character, Vector2 position)
    {
        if (characterName != character) return;
        targetPosition = position;
    }


    private new void Update()
    {
        Vector2 desiredVelocity = (targetPosition - (Vector2)transform.position).normalized * 1;
        currentVelocity = Vector2.Lerp(currentVelocity, desiredVelocity, Time.deltaTime * lerpSpeed);
        rb.velocity = currentVelocity;

        base.Update();
        // currentPosition = Vector2.Lerp(currentPosition, targetPosition, Time.deltaTime * lerpSpeed);
        // transform.position = currentPosition;
    }
}
