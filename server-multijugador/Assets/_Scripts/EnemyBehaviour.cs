using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    public int ID;

    [SerializeField] private Vector2 start;
    [SerializeField] private Vector2 end;
    [SerializeField] private float speed = 2.0f;

    private Vector2 actualPos;
    private Vector2 initialDirection;
    private Vector2 actualDirection;

    void Start()
    {
        actualPos = start;
        initialDirection = (end - start).normalized;
        actualDirection = initialDirection;

        transform.position = new Vector3(actualPos.x, actualPos.y, 0);
    }

    void Update()
    {
        UpdateEnemyPosition();
        CheckBoundsAndReverse();
    }

    public Vector2 GetActualPosition()
    {
        return actualPos;
    }

    private void UpdateEnemyPosition()
    {
        actualPos += actualDirection * speed * Time.deltaTime;
        transform.position = new Vector3(actualPos.x, actualPos.y, 0);
    }

    private void CheckBoundsAndReverse()
    {
        if (Vector2.Dot(initialDirection, end - actualPos) < 0) actualDirection = -initialDirection;
        else if (Vector2.Dot(-initialDirection, start - actualPos) < 0) actualDirection = initialDirection;
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.TryGetComponent(out SimulatedPlayerBehaviour simulatedPlayer))
        {
            Character character = simulatedPlayer.GetCharacter();
            ServerBehaviour.Instance.OnPlayerDamaged(character.name);
        }
    }
}
