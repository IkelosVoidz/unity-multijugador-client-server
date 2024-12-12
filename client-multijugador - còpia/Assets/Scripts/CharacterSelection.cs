using UnityEngine;

public class CharacterSelection : MonoBehaviour
{
    public void EscollirPersonatge(int indexPersonatge)
    {
        ClientBehaviour.Instance.ChooseCharacter(indexPersonatge);
    }
}
