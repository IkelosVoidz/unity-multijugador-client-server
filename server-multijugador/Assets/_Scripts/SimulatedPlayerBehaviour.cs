using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Ability
{
    ABILITY_RANGED,
    ABILITY_MELEE,
}

[Serializable]
public class Character
{
    public string name;
    public Sprite sprite;

    public Ability ability;
}

public class SimulatedPlayerBehaviour : MonoBehaviour
{
    public Character _character;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private IAbility ability;

    public void SetupCharacter(Character c)
    {
        _character = c;
        spriteRenderer.sprite = c.sprite;
        ability = AbilityFactory.CreateAbility(c.ability);
    }

    public void ActivateAbility(float direction)
    {
        ability.Activate(direction);
    }
}
