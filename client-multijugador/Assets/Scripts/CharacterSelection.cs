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

    public void UpdateAvaliableCharacters(PlayerReference unavailableCharacter)
    {
        int characterIndex = ClientBehaviour.Instance.GetCharacterIndexByName(unavailableCharacter.character);
        characterButtons[characterIndex].interactable = false;
    }
}
