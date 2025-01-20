
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;

public class GameBehaviour : MonoBehaviour
{
    private void OnEnable() 
    { 
        ClientBehaviour.OnOtherCharacterSelected += SpawnPlayer;
        ClientBehaviour.OnPlayerDamaged += LoseLife;
    }

    private void OnDisable() 
    {
        ClientBehaviour.OnOtherCharacterSelected -= SpawnPlayer;
        ClientBehaviour.OnPlayerDamaged -= LoseLife;
    }

    public Image[] heartImages;
    public Sprite fullHeartSprite;
    public Sprite emptyHeartSprite;

    [SerializeField] private GameObject clientPlayerPrefab;
    [SerializeField] private GameObject serverPlayerPrefab;
    [SerializeField] private GameObject arrowPrefab;

    private int lifes = 3;


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
            playerReference.character.name == ClientBehaviour.Instance.GetChosenCharacter()
                ? clientPlayerPrefab
                : serverPlayerPrefab,
            spawnPosition, Quaternion.identity);

        player.GetComponent<LoadCharacterVisuals>().LoadVisuals(playerReference.character.name);
        player.GetComponent<PlayerBehaviour>().SetupCharacter(playerReference);

        playerReference.position = playerReference.initialPosition;
        playerReference.spawned = true;

        ClientBehaviour.Instance.AddOrUpdatePlayerReference(playerReference);
    }

    public void SpawnArrow(Vector2 position)
    {
        Instantiate(arrowPrefab, Vector2.zero, Quaternion.identity);
    }

    public void LoseLife()
    {
        lifes--;
        // TODO : se envia al principio? se hace inmortal un tiempo?

        if (lifes <= 0)
        {
            //TODO : hay que hacer algo si el player se queda sin vidas, llamar a la escena you lose?
            return ;
        }

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (lifes < i + 1) heartImages[i].sprite = emptyHeartSprite;
            else heartImages[i].sprite = fullHeartSprite;
        }
    }
}