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
    [SerializeField] private Character _character;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private IAbility ability;

    private bool _abilityActive = false;
    private float direction = 0.0f;

    public void SetupCharacter(Character c)
    {
        _character = c;
        spriteRenderer.sprite = c.sprite;
        ability = AbilityFactory.CreateAbility(c.ability);
    }

    public void ActivateAbility(Vector2 position, float direction)
    {
        _abilityActive = true;
        Invoke(nameof(DeactivateAbility), 1.0f);
        this.direction = direction;
        ability.Activate(position, direction, this);
    }

    private void DeactivateAbility()
    {
        _abilityActive = false;
    }

    private void OnDrawGizmos()
    {
        if ((ability is MeleeAbility) && _abilityActive)
        {
            Vector2 boxCenter = (Vector2)transform.position + new Vector2(direction, 0);
            Vector2 boxSize = new Vector2(1, 1);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(boxCenter, boxSize);
        }
    }
}
