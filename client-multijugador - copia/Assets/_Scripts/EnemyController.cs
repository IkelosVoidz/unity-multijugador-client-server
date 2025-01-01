using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private void OnEnable() { ClientBehaviour.OnEnemyMoved += UpdateEnemyPosition; }

    private void OnDisable() { ClientBehaviour.OnEnemyMoved -= UpdateEnemyPosition; }

    [SerializeField] private GameObject _enemyPrefab;
    Dictionary<int, GameObject> m_enemyReferences = new Dictionary<int, GameObject>();

    void Start()
    {
        foreach (var enemy in ClientBehaviour.Instance.m_enemies)
        {
            GameObject createdEnemy = Instantiate(_enemyPrefab, enemy.position, Quaternion.identity);
            m_enemyReferences[enemy.enemyId] = createdEnemy;
        }
    }
    private void UpdateEnemyPosition(int enemyId, Vector2 position)
    {
        m_enemyReferences[enemyId].transform.position = position;
    }
}
