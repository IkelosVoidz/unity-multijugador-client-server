using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{
    protected string characterName;

    protected IAbility ability;

    protected Animator animator;
    protected Rigidbody2D rb;
    protected SpriteRenderer sr;


    protected virtual void Update()
    {
        animator.SetFloat("Speed", rb.velocity.magnitude);
    }

    public void SetupCharacter(PlayerReference reference)
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();
        characterName = reference.character.name;
        ability = AbilityFactory.CreateAbility(reference.character.ability);
    }

    public virtual void ActivateAbility(Vector2 position, float dir)
    {
        ability?.Activate(position, dir, this, animator);
    }

    public virtual void TakeDamage()
    {
        animator.SetTrigger("Hurt");
    }

}
