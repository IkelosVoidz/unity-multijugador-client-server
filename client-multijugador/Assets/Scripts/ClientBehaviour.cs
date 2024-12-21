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
    public string character;
    public bool spawned;
    public Vector2 position;
    public Vector2 initialPosition;
}
public class EnemyReference
{
    public int enemyId;
    public Vector2 position;
}



public class ClientBehaviour : PersistentSingleton<ClientBehaviour>
{
    //Private variables
    NetworkDriver m_Driver;
    NetworkPipeline m_Pipeline;
    NetworkConnection m_Connection;
    private string m_characterChosen = null;
    private bool m_isCharacterChosenConfirmed = false;

    //serialized variables

    [SerializeField] TMP_InputField serverIP;
    [SerializeField] TMP_InputField serverPort;
    [SerializeField] private Transform enemyTransform;

    //public variables

    public List<string> m_avaliableCharacters = new List<string>();

    public List<PlayerReference> m_players = new List<PlayerReference>();
    public List<EnemyReference> m_enemies = new List<EnemyReference>();

    //events
    public static event Action<PlayerReference> OnOtherCharacterSelected;
    public static event Action<string, Vector2> OnOtherCharacterMoved;
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
                SceneManager.LoadScene("CharacterSelection"); // En aquesta escena hi ha botons que criden 
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

    public void ChooseCharacter(int indexPersonatge)
    {
        var characterName = "Personaje" + indexPersonatge;

        m_Driver.BeginSend(m_Pipeline, m_Connection, out var writer);
        writer.WriteByte(0x01);
        writer.WriteFixedString128(characterName);
        m_Driver.EndSend(writer);
    }

    public string GetChosenCharacter()
    {
        if (m_isCharacterChosenConfirmed) return m_characterChosen;

        return null;
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
                    m_avaliableCharacters.Add(personatge);
                }
                Debug.Log("Personatges disponibles: " + string.Join(", ", m_avaliableCharacters));
                break;
            case 0x02: //Informacio de un player de una altre connexio
                string characterName = stream.ReadFixedString128().ToString();
                float x = stream.ReadFloat();
                float y = stream.ReadFloat();
                Vector2 newPos = new Vector2(x, y);

                var playerReference = m_players.FirstOrDefault(player => player.character == characterName);

                if (playerReference != null)
                {
                    playerReference.position = newPos;
                    OnOtherCharacterMoved?.Invoke(characterName, newPos);
                    return;
                }

                playerReference = new PlayerReference { character = characterName, initialPosition = newPos, position = newPos, spawned = false };
                m_players.Add(playerReference);
                OnOtherCharacterSelected?.Invoke(playerReference);

                break;

            case 0x03: // El personaje ha ido escogido correctamente

                m_characterChosen = stream.ReadFixedString128().ToString();
                float xSelf = stream.ReadFloat();
                float ySelf = stream.ReadFloat();
                Vector2 newPosSelf = new Vector2(xSelf, ySelf);
                m_isCharacterChosenConfirmed = true;
                Debug.Log("El personatge s'ha escollit correctament");

                m_players.Add(new PlayerReference { character = m_characterChosen, initialPosition = newPosSelf, position = newPosSelf, spawned = false });
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
            case 0x07:
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

                enemyReference = new EnemyReference {enemyId = enemyId, position = newPosEnemy};
                m_enemies.Add(enemyReference);
                break;

            default:
                Debug.Log($"Unknown message type: {messageType}");
                break;
        }
    }

    private void UpdateEnemyPosition(Vector2 position)
    {
        enemyTransform.position = position;
    }
    public bool IsCharacterAvailable(int index)
    {
        return m_avaliableCharacters.Contains("Personaje" + index);
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
        return m_players[m_players.FindIndex(p => p.character == characterName)].position;
    }


    public void UpdatePlayerPosition(string characterName, Vector2 position)
    {
        int index = m_players.FindIndex(p => p.character == characterName);
        m_players[index].position = position;

        m_Driver.BeginSend(m_Pipeline, m_Connection, out var writer);
        writer.WriteByte(0x06);
        writer.WriteFloat(position.x);
        writer.WriteFloat(position.y);
        m_Driver.EndSend(writer);
    }
}
