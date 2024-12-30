using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Character
{
    public string name;
    public Sprite sprite;
}

public class SimulatedPlayerBehaviour : MonoBehaviour
{
    public string characterName;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public void SetupCharacter(Character c)
    {
        characterName = c.name;
        spriteRenderer.sprite = c.sprite;
    }
}
