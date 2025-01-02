using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAbility
{
    void Activate(float direction, MonoBehaviour owner); //-1 left, 1 right
}
