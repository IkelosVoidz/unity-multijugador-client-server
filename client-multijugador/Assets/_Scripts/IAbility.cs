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
    void Activate(float direction); //-1 left, 1 right
}
