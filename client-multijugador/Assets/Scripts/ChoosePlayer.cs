using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChoosePlayer : MonoBehaviour
{
    public void EscollirPersonatge(int indexPersonatge)
    {
        ClientBehaviour.Instance.EscollirPersonatge(indexPersonatge);
    }
}
