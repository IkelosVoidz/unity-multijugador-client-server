
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
        ClientBehaviour.Instance.m_players.ForEach(player =>
        {
            if (player.spawned) return;
            SpawnPlayer(player);
        });
    }

    public void SpawnPlayer(PlayerReference playerReference)
    {
        var player = Instantiate(
            playerReference.character == ClientBehaviour.Instance.GetChosenCharacter()
                ? clientPlayerPrefab
                : serverPlayerPrefab,
            playerReference.initialPosition, Quaternion.identity);

        player.GetComponent<LoadCharacterVisuals>().loadCharacterVisuals(playerReference.character);

        playerReference.position = playerReference.initialPosition;
        playerReference.spawned = true;

        ClientBehaviour.Instance.AddOrUpdatePlayerReference(playerReference);
    }
}