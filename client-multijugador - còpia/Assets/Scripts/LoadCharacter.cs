using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadCharacter : MonoBehaviour
{
    [SerializeField] private TMP_Text characterText;
    [SerializeField] private Image characterImage;

    [SerializeField] private List<Sprite> characterSprites = new List<Sprite>();


    void Start()
    {
        string chosenCharacter = ClientBehaviour.Instance.GetChosenCharacter();

        if (chosenCharacter == null) return;

        characterText.text = chosenCharacter;

        int characterIndex = int.Parse(chosenCharacter.Substring("Personaje".Length));

        characterImage.sprite = characterSprites[characterIndex - 1];
        characterImage.SetNativeSize();
    }
}