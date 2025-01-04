using UnityEngine;

public class RangedAbility : IAbility
{
    public void Activate(float direction, MonoBehaviour owner)
    {
        ClientBehaviour.Instance.SpawnProjectile(owner.transform.position, direction);
    }
}

public class MeleeAbility : IAbility
{
    public void Activate(float direction, MonoBehaviour owner)
    {
        Debug.Log("Melee ability direction = " + ((direction == 1) ? "Right" : "Left"));
    }
}