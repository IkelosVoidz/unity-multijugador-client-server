
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

public class GameBehaviour : MonoBehaviour
{

    private void OnEnable() { ClientBehaviour.OnOtherCharacterSelected += SpawnPlayer; }


    private void OnDisable() { ClientBehaviour.OnOtherCharacterSelected -= SpawnPlayer; }

    [SerializeField] private GameObject clientPlayerPrefab;
    [SerializeField] private GameObject serverPlayerPrefab;


    void Start()
    {
        foreach (var player in new List<PlayerReference>(ClientBehaviour.Instance.m_players)) //iterem sobre una copia perque es modificara la llista original
        {
            if (player.spawned) return;
            SpawnPlayer(player);
        }
    }

    public void SpawnPlayer(PlayerReference playerReference)
    {
        if (playerReference.spawned) return;


        Vector2 spawnPosition = playerReference.initialPosition != playerReference.position
            ? playerReference.position
            : playerReference.initialPosition;

        var player = Instantiate(
            playerReference.character == ClientBehaviour.Instance.GetChosenCharacter()
                ? clientPlayerPrefab
                : serverPlayerPrefab,
            spawnPosition, Quaternion.identity);

        player.GetComponent<LoadCharacterVisuals>().loadCharacterVisuals(playerReference.character);
        player.GetComponent<PlayerBehaviour>().characterName = playerReference.character;

        playerReference.position = playerReference.initialPosition;
        playerReference.spawned = true;

        ClientBehaviour.Instance.AddOrUpdatePlayerReference(playerReference);
    }
}