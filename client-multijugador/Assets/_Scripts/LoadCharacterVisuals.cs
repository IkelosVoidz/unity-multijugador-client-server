using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadCharacterVisuals : MonoBehaviour
{
    [SerializeField] List<GameObject> characterVisuals = new List<GameObject>();
    public void LoadVisuals(string characterName)
    {
        int characterIndex = ClientBehaviour.Instance.GetCharacterIndexByName(characterName);
        Instantiate(characterVisuals[characterIndex], transform.position, Quaternion.identity, transform);
    }
}
