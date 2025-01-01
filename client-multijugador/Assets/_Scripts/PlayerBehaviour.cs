using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{
    protected string characterName;

    protected IAbility ability;

    protected Animator animator;
    protected Rigidbody2D rb;


    protected virtual void Update()
    {
        animator.SetFloat("Speed", rb.velocity.magnitude);
    }

    public void SetupCharacter(PlayerReference reference)
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        characterName = reference.character.name;
        ability = AbilityFactory.CreateAbility(reference.character.ability);
    }

    public virtual void ActivateAbility(float dir)
    {
        animator.SetTrigger("Attack");
        ability?.Activate(dir);
    }

    public virtual void TakeDamage()
    {
        animator.SetTrigger("Hurt");
    }

}
