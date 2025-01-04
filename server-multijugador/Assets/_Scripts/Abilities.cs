
using UnityEngine;

public class RangedAbility : IAbility
{
    public void Activate(Vector2 position, float direction, MonoBehaviour owner)
    {
        ServerBehaviour.Instance.CreateProjectile(position, direction);
    }
}

public class MeleeAbility : IAbility
{
    public void Activate(Vector2 position, float direction, MonoBehaviour owner)
    {
        Debug.Log("Melee ability direction = " + ((direction == 1) ? "Right" : "Left"));

        Vector2 boxCenter = position + new Vector2(direction * 1, 0);
        Vector2 boxSize = new Vector2(1, 1);

        Collider2D[] hitColliders = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0, 1 << LayerMask.NameToLayer("Enemy"));
        foreach (var hitCollider in hitColliders)
        {
            ServerBehaviour.Instance.NotifyEnemyHit(hitCollider.gameObject);
        }
    }
}