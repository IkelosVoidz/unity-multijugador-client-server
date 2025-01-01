using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerPlayerBehaviour : PlayerBehaviour
{
    private Vector2 currentPosition;
    private Vector2 targetPosition;
    [SerializeField] private float lerpSpeed = 5f;

    private void Start()
    {
        currentPosition = transform.position;
        targetPosition = transform.position;
    }

    private void OnEnable() { ClientBehaviour.OnOtherCharacterMoved += UpdatePositionFromServer; }
    private void OnDisable() { ClientBehaviour.OnOtherCharacterMoved -= UpdatePositionFromServer; }

    private void UpdatePositionFromServer(string character, Vector2 position)
    {
        if (characterName != character) return;
        targetPosition = position;
    }

    public void ActivateAbility(float dir)
    {
        ability?.Activate(dir);
    }

    private void Update()
    {
        currentPosition = Vector2.Lerp(currentPosition, targetPosition, Time.deltaTime * lerpSpeed);
        transform.position = currentPosition;
    }
}
