using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;
using System;
using System.Linq;


public enum ClientState
{
    SETUP,
    GAME,
}

public class PlayerReference
{
    public string character;
    public bool spawned;
    public Vector2 position;
    public Vector2 initialPosition;
}


public class ClientBehaviour : PersistentSingleton<ClientBehaviour>
{
    NetworkDriver m_Driver;
    NetworkPipeline m_Pipeline;
    NetworkConnection m_Connection;
    [SerializeField] TMP_InputField serverIP;
    [SerializeField] TMP_InputField serverPort;
    [SerializeField] ClientState m_clientState = ClientState.SETUP;

    private string m_characterChosen = null;
    private bool m_isCharacterChosenConfirmed = false;

    List<string> m_avaliableCharacters = new List<string>();

    public List<PlayerReference> m_players = new List<PlayerReference>();
    public static event Action<PlayerReference> OnOtherCharacterSelected;


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

                var newlySelectedCharacter = m_avaliableCharacters.Except(prevAvaliableCharacters).FirstOrDefault();
                break;
            case 0x02: //Informacio de un player de una altre connexio
                string characterName = stream.ReadFixedString128().ToString();
                float x = stream.ReadFloat();
                float y = stream.ReadFloat();

                if (!m_players.Any(player => player.character == characterName))
                {
                    var playerReference = new PlayerReference { character = characterName, initialPosition = new Vector2(x, y), spawned = false };
                    m_players.Add(playerReference);
                    OnOtherCharacterSelected?.Invoke(playerReference);
                }

                //todo update positions of other players

                break;

            case 0x03: // El personaje ha ido escogido correctamente

                m_characterChosen = stream.ReadFixedString128().ToString();
                float xSelf = stream.ReadFloat();
                float ySelf = stream.ReadFloat();
                m_isCharacterChosenConfirmed = true;
                Debug.Log("El personatge s'ha escollit correctament");

                m_players.Add(new PlayerReference { character = m_characterChosen, initialPosition = new Vector2(xSelf, ySelf), spawned = false });
                SceneManager.LoadScene("GameScene");

                break;

            case 0x04: // El personaje ha ido escogido por otra conexion.
                m_characterChosen = "";
                m_isCharacterChosenConfirmed = false;

                Debug.Log("El personatge ha estat escollit per una altra connexió");
                break;

            default:
                Debug.Log($"Unknown message type: {messageType}");
                break;
        }
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

    public void UpdatePlayerPosition(string characterName, Vector2 position)
    {
        int index = m_players.FindIndex(p => p.character == characterName);
        m_players[index].position = position;

        //todo send updated position to server 
    }
}
