using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowBehaviour : MonoBehaviour
{
    [SerializeField] private float uptime = 5f;

    private void Start()
    {
        Invoke("DestroyProjectile", uptime);
    }

    private void DestroyProjectile()
    {
        ServerBehaviour.Instance.NotifyProjectileDeletion();
        Destroy(gameObject);
    }


    private void OnCollisionEnter2D(Collision2D other)
    {
        bool isEnemy = other.gameObject.layer == LayerMask.NameToLayer("Enemy");

        if (isEnemy)
        {
            ServerBehaviour.Instance.NotifyEnemyHit(other.gameObject);
        }

        DestroyProjectile();
    }
}
