using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadCharacter : MonoBehaviour
{
    public TMP_Text characterText;
    public Image imageCharacter;

    public Sprite personatge1;
    public Sprite personatge2;
    public Sprite personatge3;
    public Sprite personatge4;

    private
    // Start is called before the first frame update
    void Start()
    {
        string personatgeEscollit = ClientBehaviour.Instance.GetPersonatgeEscollit();

        if (personatgeEscollit != null)
        {
            characterText.text = personatgeEscollit;

            switch (personatgeEscollit)
            {
                case "Personaje1":
                    imageCharacter.sprite = personatge1; break;
                case "Personaje2":
                    imageCharacter.sprite = personatge2; break;
                case "Personaje3":
                    imageCharacter.sprite = personatge3; break;
                case "Personaje4":
                    imageCharacter.sprite = personatge4; break;
            }

            imageCharacter.SetNativeSize();
        }
    }
}
