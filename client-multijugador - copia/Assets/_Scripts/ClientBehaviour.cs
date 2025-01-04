using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;
using System;
using System.Linq;

public class PlayerReference
{
    public Character character;
    public bool spawned;
    public Vector2 position;
    public Vector2 initialPosition;
    public Vector2 velocity;

}
public class EnemyReference
{
    public int enemyId;
    public Vector2 position;
}

public class Character
{
    public string name;
    public Ability ability;
}

public class ClientBehaviour : PersistentSingleton<ClientBehaviour>
{
    //Private variables
    NetworkDriver m_Driver;
    NetworkPipeline m_Pipeline;
    NetworkConnection m_Connection;
    private string m_characterChosen = null;
    private bool m_isCharacterChosenConfirmed = false;

    private bool m_canSpawnProjectile = true;

    //serialized variables

    [SerializeField] TMP_InputField serverIP;
    [SerializeField] TMP_InputField serverPort;

    [SerializeField] GameObject m_projectilePrefab;
    private GameObject m_projectileRef;

    //public variables

    public List<Character> m_avaliableCharacters = new List<Character>();

    public List<PlayerReference> m_players = new List<PlayerReference>();
    public List<EnemyReference> m_enemies = new List<EnemyReference>();

    //events
    public static event Action<PlayerReference> OnOtherCharacterSelected;
    public static event Action<string, Vector2, Vector2> OnOtherCharacterMoved;

    public static event Action<float> OnOtherCharacterAbilityActivated;
    public static event Action<Vector2> OnSelfMoved;
    public static event Action<int, Vector2> OnEnemyMoved;

    void Start()
    {
        m_Driver = NetworkDriver.Create();
        m_Pipeline = m_Driver.CreatePipeline( // Configura el pipeline
            typeof(ReliableSequencedPipelineStage)
        );
    }

    public void Connectar()
    {
        var endpoint = NetworkEndpoint.Parse(serverIP.text, Convert.ToUInt16(serverPort.text));
        m_Connection = m_Driver.Connect(endpoint);

        Debug.Log($"Intentando conectar al servidor en {serverIP.text}:{serverPort.text}");
    }

    void OnDestroy()
    {
        m_Driver.Disconnect(m_Connection);
        m_Driver.Dispose();
    }

    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;
        while ((cmd = m_Connection.PopEvent(m_Driver, out stream, out var pipeline)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                Debug.Log("Conexión establecida con el servidor.");
                SceneManager.LoadScene("CharacterSelection");
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                PayloadDeconstructor(stream);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Desconexión del servidor.");
                m_Connection = default;
            }
        }
    }

    public string GetChosenCharacter()
    {
        if (m_isCharacterChosenConfirmed) return m_characterChosen;

        return null;
    }

    public Ability GetAbilityFromCharacterName(string characterName)
    {
        var abilityUserReference = m_players.FirstOrDefault(player => player.character.name == characterName);
        return abilityUserReference.character.ability;
    }

    void PayloadDeconstructor(DataStreamReader stream)
    {
        byte messageType = stream.ReadByte();
        switch (messageType)
        {
            case 0x00: // Llista de personatges disponibles

                int nAvaliableCharacters = stream.ReadInt();

                var prevAvaliableCharacters = m_avaliableCharacters;

                for (int i = 0; i < nAvaliableCharacters; i++)
                {
                    string personatge = stream.ReadFixedString128().ToString();
                    Ability ability = (Ability)stream.ReadInt();
                    m_avaliableCharacters.Add(new Character { name = personatge, ability = ability });
                }
                Debug.Log("Personatges disponibles: " + string.Join(", ", m_avaliableCharacters.Select(c => $"{c.name} ({c.ability})")));
                break;
            case 0x02: //Informacio de un player de una altre connexio
                string characterName = stream.ReadFixedString128().ToString();
                float x = stream.ReadFloat();
                float y = stream.ReadFloat();
                float vx = stream.ReadFloat();
                float vy = stream.ReadFloat();
                Vector2 newPos = new Vector2(x, y);
                Vector2 newVel = new Vector2(vx, vy);

                var playerReference = m_players.FirstOrDefault(player => player.character.name == characterName);

                if (playerReference != null)
                {
                    playerReference.position = newPos;
                    OnOtherCharacterMoved?.Invoke(characterName, newPos, newVel);
                    return;
                }

                Character otherSelectedCharacter = m_avaliableCharacters.FirstOrDefault(c => c.name == characterName);

                playerReference = new PlayerReference { character = otherSelectedCharacter, initialPosition = newPos, position = newPos, spawned = false, velocity = new Vector2(vx, vy) };
                m_players.Add(playerReference);
                OnOtherCharacterSelected?.Invoke(playerReference);

                break;

            case 0x03: // El personaje ha sido escogido correctamente

                m_characterChosen = stream.ReadFixedString128().ToString();
                float xSelf = stream.ReadFloat();
                float ySelf = stream.ReadFloat();
                Vector2 newPosSelf = new Vector2(xSelf, ySelf);
                m_isCharacterChosenConfirmed = true;
                Debug.Log("El personatge s'ha escollit correctament");

                Character selectedCharacter = m_avaliableCharacters.FirstOrDefault(c => c.name == m_characterChosen);


                m_players.Add(new PlayerReference { character = selectedCharacter, initialPosition = newPosSelf, position = newPosSelf, spawned = false });
                SceneManager.LoadScene("GameScene");

                break;

            case 0x04: // El personaje ha ido escogido por otra conexion.
                m_characterChosen = "";
                m_isCharacterChosenConfirmed = false;

                Debug.Log("El personatge ha estat escollit per una altra connexió");
                break;

            case 0x05: //El servidor t'ha capat la posicio perque estas hackejant
                Debug.Log("El servidor t'ha capat la posicio perque estas hackejant");

                float xSelf2 = stream.ReadFloat();
                float ySelf2 = stream.ReadFloat();
                Vector2 newPosSelf2 = new Vector2(xSelf2, ySelf2);
                OnSelfMoved?.Invoke(newPosSelf2);
                break;

            case 0x08: //The server informs that a player has thrown an ability
                string abilityUser = stream.ReadFixedString128().ToString();
                Ability abilityUsed = GetAbilityFromCharacterName(abilityUser);

                float direction = stream.ReadFloat();

                Debug.Log($"Server reported character: {abilityUser} has used ability {abilityUsed} with direction {direction}");

                OnOtherCharacterAbilityActivated?.Invoke(direction);

                break;
            case 0x09: //The server sends the position of an enemy
                int enemyId = stream.ReadInt();
                float enemyX = stream.ReadFloat();
                float enemyY = stream.ReadFloat();
                Vector2 newPosEnemy = new Vector2(enemyX, enemyY);

                var enemyReference = m_enemies.FirstOrDefault(enemy => enemy.enemyId == enemyId);

                if (enemyReference != null)
                {
                    enemyReference.position = newPosEnemy;
                    OnEnemyMoved?.Invoke(enemyId, newPosEnemy);
                    return;
                }

                enemyReference = new EnemyReference { enemyId = enemyId, position = newPosEnemy };
                m_enemies.Add(enemyReference);
                break;
            case 0x10: //The server says that an enemy has recieved damage, it sends the id of the enemy
                int enemyIdRecievedDamage = stream.ReadInt();
                Debug.Log($"Server reported Enemy: {enemyIdRecievedDamage} recieved damage.");
                break;
            case 0x11: // The server says a player has recieved damage
                string collidedCharacter = stream.ReadFixedString128().ToString();

                Debug.Log($"Server reported collision: {collidedCharacter} with Enemy.");
                break;
            case 0x12:
                Destroy(m_projectileRef);
                m_canSpawnProjectile = true;
                break;
            case 0x13:// The server says a character has crossed the line i cant fight this time now i can feel the liiiine shine on my faaace did i diisappoiint you , will they still let me oover, if ii cross the liiine
                Debug.Log($"Server reported a character has crossed the line.");
                break;

            default:
                Debug.Log($"Unknown message type: {messageType}");
                break;
        }
    }

    public void ChooseCharacter(int indexPersonatge) //0x01
    {
        var characterName = "Personaje" + indexPersonatge;

        m_Driver.BeginSend(m_Pipeline, m_Connection, out var writer);
        writer.WriteByte(0x01);
        writer.WriteFixedString128(characterName);
        m_Driver.EndSend(writer);
    }

    public void UpdatePlayerPosition(string characterName, Vector2 position, Vector2 velocity) //0x06
    {
        int index = m_players.FindIndex(p => p.character.name == characterName);
        m_players[index].position = position;

        m_Driver.BeginSend(m_Pipeline, m_Connection, out var writer);
        writer.WriteByte(0x06);
        writer.WriteFloat(position.x);
        writer.WriteFloat(position.y);
        writer.WriteFloat(velocity.x);
        writer.WriteFloat(velocity.y);
        m_Driver.EndSend(writer);
    }

    public void SendAbility(float direction) //0x08
    {
        m_Driver.BeginSend(m_Pipeline, m_Connection, out var writer);
        writer.WriteByte(0x07);
        writer.WriteFloat(direction);
        m_Driver.EndSend(writer);
    }

    public bool IsCharacterAvailable(int index)
    {
        return m_avaliableCharacters.Any(p => p.name == "Personaje" + index);
    }

    public int GetCharacterIndexByName(string characterName)
    {
        return int.Parse(characterName.Substring("Personaje".Length)) - 1;
    }

    public void AddOrUpdatePlayerReference(PlayerReference playerReference)
    {
        int index = m_players.FindIndex(p => p.character == playerReference.character);
        if (index == -1)
        {
            m_players.Add(playerReference);
        }
        else
        {
            m_players[index] = playerReference;
        }
    }


    public Vector2 GetLastSentPosition(string characterName)
    {
        return m_players[m_players.FindIndex(p => p.character.name == characterName)].position;
    }


    public void SpawnProjectile(Vector2 position, float direction)
    {
        if (!m_canSpawnProjectile) return;

        GameObject projectile = Instantiate(m_projectilePrefab, position, Quaternion.identity);
        projectile.GetComponent<Rigidbody2D>().AddForce(new Vector2(direction, 0) * 5, ForceMode2D.Impulse);
        if (direction > 0) projectile.transform.localScale = new Vector3(-1, 1, 1);

        m_canSpawnProjectile = false;
    }
}
