using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSelection : MonoBehaviour
{
    public void EscollirPersonatge(int indexPersonatge)
    {
        ClientBehaviour.Instance.ChooseCharacter(indexPersonatge);
    }
}
