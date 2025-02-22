using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientPlayerBehaviour : PlayerBehaviour
{
    [SerializeField] private float positionThreshold = 0.05f; // Minimum distance to trigger an update instantly
    [SerializeField] private float updateInterval = 0.1f; // Time in seconds between updates
    private float updateTimer;

    public float dir = 1.0f;

    private void Start()
    {
        updateTimer = 0f; // Initialize to 0 to ensure the first update occurs immediately if needed
    }

    private new void Update()
    {
        base.Update();
        // Decrement timer but clamp it to prevent it from going negative
        updateTimer = Mathf.Max(0f, updateTimer - Time.deltaTime);
        sr.flipX = dir < 0f;

        if (Input.GetMouseButtonDown(0))
        {
            ActivateAbility(transform.position, dir);
        }
    }

    public bool ShouldUpdatePosition(Vector2 lastSentPosition, Vector2 currentPosition)
    {
        // Check if position has changed substantially
        bool positionChanged = Vector2.Distance(currentPosition, lastSentPosition) > positionThreshold;

        // Check if the timer has elapsed
        bool timerElapsed = updateTimer <= 0f;

        // Perform the update if:
        // 1. Position has changed substantially OR
        // 2. Timer has elapsed AND
        // 3. Position is not identical to the last sent position
        if (timerElapsed && currentPosition != lastSentPosition)
        {
            // Reset the timer
            updateTimer = updateInterval;
            return true;
        }
        return false;
    }

    public void UpdateServerPosition(Vector2 position)
    {
        var lastSentPosition = ClientBehaviour.Instance.GetLastSentPosition(characterName);
        if (ShouldUpdatePosition(lastSentPosition, position))
        {
            ClientBehaviour.Instance.UpdatePlayerPosition(characterName, position, rb.velocity);
        }
    }

    public override void TakeDamage()
    {
        base.TakeDamage();
    }
}
