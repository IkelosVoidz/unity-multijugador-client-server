using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelection : MonoBehaviour
{
    [SerializeField] List<Button> characterButtons;

    private void OnEnable() { ClientBehaviour.OnOtherCharacterSelected += UpdateAvaliableCharacters; }

    private void OnDisable() { ClientBehaviour.OnOtherCharacterSelected -= UpdateAvaliableCharacters; }

    public void EscollirPersonatge(int indexPersonatge)
    {
        ClientBehaviour.Instance.ChooseCharacter(indexPersonatge);
    }

    // private void Start()
    // {
    //     for (int i = 0; i < characterButtons.Count; i++)
    //     {
    //         characterButtons[i].interactable = ClientBehaviour.Instance.IsCharacterAvailable(i + 1);
    //     }
    // }

    public void UpdateAvaliableCharacters(PlayerReference unavailableCharacter)
    {
        int characterIndex = ClientBehaviour.Instance.GetCharacterIndexByName(unavailableCharacter.character.name);
        characterButtons[characterIndex].interactable = false;
    }
}
