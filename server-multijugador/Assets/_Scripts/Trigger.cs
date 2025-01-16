using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Trigger : MonoBehaviour
{
    [SerializeField] UnityEvent onTriggerEnter;


    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("FIIIIIIIIIIIIIIIIIIIIINAAAAAAAAAAAAAAAAAAAAAAAALLLLLLLLLLLL");
        onTriggerEnter.Invoke();
    }
}