using UnityEngine;

public class RangedAbility : IAbility
{
    public void Activate(float direction, MonoBehaviour owner)
    {
        Debug.Log("Ranged ability direction = " + ((direction == 1) ? "Right" : "Left"));
    }
}

public class MeleeAbility : IAbility
{
    public void Activate(float direction, MonoBehaviour owner)
    {
        Debug.Log("Melee ability direction = " + ((direction == 1) ? "Right" : "Left"));

        Vector2 boxCenter = (Vector2)owner.transform.position + new Vector2(direction * 1, 0);
        Vector2 boxSize = new Vector2(1, 1);

        Collider2D[] hitColliders = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0, 1 << LayerMask.NameToLayer("Enemy"));
        foreach (var hitCollider in hitColliders)
        {
            Debug.Log("Hit: " + hitCollider.name);
        }
    }
}