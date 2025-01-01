using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimEventsThingDoer : MonoBehaviour
{
    public static event Action OnArrowFired;

    public void OnAnimArrowFired()
    {
        Debug.Log("Arrow fired");
        OnArrowFired?.Invoke();
    }
}
