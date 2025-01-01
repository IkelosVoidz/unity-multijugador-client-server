using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{
    public string characterName;

    public IAbility ability;


    public void SetupCharacter(PlayerReference reference)
    {
        characterName = reference.character.name;
        ability = AbilityFactory.CreateAbility(reference.character.ability);
    }
}
