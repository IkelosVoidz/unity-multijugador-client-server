using UnityEngine;

public class RangedAbility : IAbility
{
    public void Activate(float direction)
    {
        Debug.Log("Ranged ability direction = " + ((direction == 1) ? "Right" : "Left"));
    }
}

public class MeleeAbility : IAbility
{
    public void Activate(float direction)
    {
        Debug.Log("Melee ability direction = " + ((direction == 1) ? "Right" : "Left"));
    }
}