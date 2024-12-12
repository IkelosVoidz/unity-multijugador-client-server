using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadCharacter : MonoBehaviour
{
    [SerializeField] private SpriteRenderer characterSprite;

    [SerializeField] private List<Sprite> characterSprites = new List<Sprite>();


    void Start()
    {
        string chosenCharacter = ClientBehaviour.Instance.GetChosenCharacter();

        if (chosenCharacter == null) return;
        int characterIndex = int.Parse(chosenCharacter.Substring("Personaje".Length));
        characterSprite.sprite = characterSprites[characterIndex - 1];
    }
}