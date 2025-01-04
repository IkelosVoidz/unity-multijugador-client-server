using UnityEngine;

public class RangedAbility : IAbility
{
    public void Activate(Vector2 position, float direction, MonoBehaviour owner, Animator animator)
    {
        if (!ClientBehaviour.Instance.CanSpawnProjectile()) return;

        animator.SetTrigger("Attack");
        ClientBehaviour.Instance.SendAbility(position, direction);
        ClientBehaviour.Instance.SpawnProjectile(owner.transform.position, direction);
    }
}

public class MeleeAbility : IAbility
{
    public void Activate(Vector2 position, float direction, MonoBehaviour owner, Animator animator)
    {
        animator.SetTrigger("Attack");
        ClientBehaviour.Instance.SendAbility(position, direction);
        Debug.Log("Melee ability direction = " + ((direction == 1) ? "Right" : "Left"));
    }
}