using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Ability
{
    ABILITY_RANGED,
    ABILITY_MELEE,
}

public interface IAbility
{
    void Activate(Vector2 position, float direction, MonoBehaviour owner, Animator animator); //-1 left, 1 right
}
