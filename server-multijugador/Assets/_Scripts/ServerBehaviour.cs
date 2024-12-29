using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using System.Collections.Generic;
using TMPro;
using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;
public class PlayerReference
{
    public string character;
    public Transform transform;

}

public class ServerBehaviour : MonoBehaviour
{
    [SerializeField] private TMP_Text IP_Text;

    NetworkDriver m_Driver;
    NetworkPipeline m_Pipeline;
    NativeList<NetworkConnection> m_Connections;
    [SerializeField]
    List<Character> m_characters = new List<Character>
        {
            new () { name = "Personaje1" },
            new () { name = "Personaje2" },
            new () { name = "Personaje3" },
            new () { name = "Personaje4" }
        };

    [SerializeField, ReadOnly] private List<string> m_availableCharacters;
    [SerializeField] List<Transform> m_initialPlayerPositions = new List<Transform>();
    [SerializeField] public float moveDistanceThreshold = 1f;

    //TODO : mover a EnemyBehaviour

    [SerializeField] private Transform enemyTransform; // Transform del enemigo
    [SerializeField] private float enemySpeed = 2.0f;   // Velocidad del enemigo
    private Vector2 enemyDirection = Vector2.right;    // Dirección inicial del movimiento

    Dictionary<NetworkConnection, PlayerReference> m_playerReferences = new Dictionary<NetworkConnection, PlayerReference>();

    public int GetCharacterIndexByName(string name) => m_characters.FindIndex(c => c.name == name);


    private void Awake()
    {
        m_availableCharacters = new List<string>(m_characters.Select(c => c.name));
    }

    [SerializeField] ushort Port = 7777;

    void Start()
    {
        m_Driver = NetworkDriver.Create();
        m_Pipeline = m_Driver.CreatePipeline(
            typeof(ReliableSequencedPipelineStage)
        );
        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

        var endpoint = NetworkEndpoint.AnyIpv4.WithPort(Port);
        if (m_Driver.Bind(endpoint) != 0)
        {
            Debug.LogError("Error al vincular el servidor al puerto." + Port);
            return;
        }
        m_Driver.Listen();
        Debug.Log($"Servidor iniciado en la IP: {GetLocalIPAddress()} y puerto {endpoint.Port}");
        IP_Text.text = $"IP: {GetLocalIPAddress()}:{endpoint.Port}";
    }

    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new System.Exception("No network adapters with an IPv4 address in the system!");
    }

    void OnDestroy()
    {
        if (m_Driver.IsCreated)
        {
            m_Driver.Dispose();
            m_Connections.Dispose();
        }
    }

    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                m_Connections.RemoveAtSwapBack(i);
                i--;
            }
        }

        NetworkConnection c;
        while ((c = m_Driver.Accept()) != default)
        {
            m_Connections.Add(c);
            Debug.Log("Conexión aceptada.");
            SendAvailableCharacters(c);
        }


        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (m_Connections[i].IsCreated)
            {
                DataStreamReader stream;
                NetworkEvent.Type cmd;
                while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) != NetworkEvent.Type.Empty)
                {
                    if (cmd == NetworkEvent.Type.Data)
                    {
                        ProcessClientMessages(m_Connections[i], stream);
                    }
                    else if (cmd == NetworkEvent.Type.Disconnect)
                    {
                        Debug.Log($"Client {i} disconnected from the server.");
                        m_Connections[i] = default;
                        break;
                    }
                }
            }
        }

        //TODO : pol saca toda esta logica de aqui que aqui no debe llegar si no hay players conectados y este script solo se deberia de encargar de recibir y enviar mensajes, nada de controlar los enemigos
        //TODO : metelo en otro script EnemyBehaviour , meteselo a cada enemigo de la escena y envia la info a este, ahora es un singleton (antes no lo era) 
        //TODO : mueve al enemigo desde su update y controlas el envio de la posicion desde aqui como ya lo haces pero sin enviarla a cada frame, controlala como hice yo con los players
        //TODO : puedes hacer un array de gameObjects (los enemigos) , crear un array de Vector2 con la misma length del array de gameObjects y metiendole la posicion inicial de cada enemigo
        //TODO : y ir comparando si se han movido X cantidad desde la ultima posicion enviada, entonces envias, actualizas la ultima posicion enviada, y palante 
        //TODO : finalmente llama a notifyColision desde el OnTriggerEnter, para el personaje correcto haces un tryGetComponent RemotePlayerBehaviour y si lo encuentra buscas el .character y se lo pasas al metodo*/
        //TODO : creas un prefab enemigo le metes sprite renderer collider con trigger y el script EnemyBehaviour y creas varios enemigos, asegurate de actualizar el array serializado

        UpdateEnemyPosition(); // Actualiza la posición del enemigo //Todo : esto pa fuera

        CheckPlayerEnemyCollisions(); //todo : esto pa fuera

        //TODO : esto puede estar dentro del primer for de las conexiones, dentro del isCreated , mas clean
        // Envía la posición del enemigo a los clientes
        foreach (var connection in m_Connections)
        {
            if (connection.IsCreated)
                SendEnemyPosition(connection);
        }
    }

    //Todo : este metodo da igual 
    private void CheckPlayerEnemyCollisions()
    {
        foreach (var player in m_playerReferences.Values)
        {
            Vector2 playerPosition = player.transform.position;
            Vector2 enemyPosition = enemyTransform.position;

            // Assuming a radius-based collision detection
            float playerRadius = 0.5f; // Adjust as needed
            float enemyRadius = 0.5f;  // Adjust as needed

            if (Vector2.Distance(playerPosition, enemyPosition) < playerRadius + enemyRadius)
            {
                HandlePlayerEnemyCollision(player);
            }
        }
    }

    //TODO: este metodo da igual 
    private void HandlePlayerEnemyCollision(PlayerReference player)
    {
        Debug.Log($"Player {player.character} collided with the enemy!");

        foreach (var connection in m_Connections)
        {
            if (connection.IsCreated)
                NotifyCollision(connection, player.character);
        }
    }


    //TODO : llama a este metodo desde EnemyBehaviour
    private void NotifyCollision(NetworkConnection connection, string character)
    {
        //para todas las conexiones 
        m_Driver.BeginSend(m_Pipeline, connection, out var writer);
        writer.WriteByte(0x011); // Message type for collision
        writer.WriteFixedString128(character);
        m_Driver.EndSend(writer);
    }

    private void UpdateEnemyPosition()
    {
        //TODO : hacer por cada enemigo en el array
        Vector2 currentPosition = enemyTransform.position;
        currentPosition += enemyDirection * enemySpeed * Time.deltaTime;

        if (currentPosition.x > 10 || currentPosition.x < -10)
            enemyDirection = -enemyDirection;

        enemyTransform.position = currentPosition;
    }

    private void SendEnemyPosition(NetworkConnection connection)
    {
        m_Driver.BeginSend(m_Pipeline, connection, out var writer);
        writer.WriteByte(0x09); // Tipo de mensaje: posición del enemigo
        writer.WriteInt(0);
        writer.WriteFloat(enemyTransform.position.x);
        writer.WriteFloat(enemyTransform.position.y);
        m_Driver.EndSend(writer);
    }
    void SendAvailableCharacters(NetworkConnection connection)
    {
        m_Driver.BeginSend(m_Pipeline, connection, out var writer);
        writer.WriteByte(0x00); // Tipo de mensaje: Lista de personajes disponibles
        writer.WriteInt(m_availableCharacters.Count);

        foreach (var character in m_availableCharacters)
        {
            writer.WriteFixedString128(character);
        }
        m_Driver.EndSend(writer);
        Debug.Log("Lista de personajes enviados.");
    }

    void ProcessClientMessages(NetworkConnection connection, DataStreamReader stream)
    {
        byte messageType = stream.ReadByte();
        switch (messageType)
        {
            case 0x01: // Selección de personaje
                string selectedCharacter = stream.ReadFixedString128().ToString();
                if (!m_availableCharacters.Contains(selectedCharacter)) //Si ja esta seleccionat
                {
                    Debug.Log("Cliente seleccionó un personaje ya seleccionado");
                    CharacterSelectionResponse(connection, null);
                    return;
                }

                m_availableCharacters.Remove(selectedCharacter);

                var playerReference = new PlayerReference
                {
                    character = selectedCharacter,
                    transform = m_initialPlayerPositions[m_Connections.IndexOf(connection)]
                };
                m_playerReferences[connection] = playerReference;


                Debug.Log($"Cliente seleccionó: {selectedCharacter}");
                CharacterSelectionResponse(connection, m_playerReferences[connection]);


                //Setup character in server scene


                m_playerReferences[connection].transform.gameObject.GetComponent<SimulatedPlayerBehaviour>().SetupCharacter(m_characters[GetCharacterIndexByName(selectedCharacter)]);

                //Notify other clients
                foreach (var c in m_Connections)
                {
                    if (c != connection && c.IsCreated) SendCharacterInfo(c, m_playerReferences[connection]);
                }

                break;

            case 0x06:
                var x = stream.ReadFloat();
                var y = stream.ReadFloat();

                Vector2 newPos = new Vector2(x, y);
                Vector2 lastPos = m_playerReferences[connection].transform.position;


                if (Vector2.Distance(newPos, lastPos) > moveDistanceThreshold)
                {
                    Vector2 direction = (newPos - lastPos).normalized;
                    newPos = lastPos + direction * moveDistanceThreshold; //Capem el moviment al max threshold 

                    SendCorrectPositionToClients(connection, newPos);
                }


                var index = m_Connections.IndexOf(connection);
                Debug.Log($"Client {index} moved {m_playerReferences[connection].character} to ({newPos.x}, {newPos.y})");

                m_playerReferences[connection].transform.SetPositionAndRotation(newPos, m_playerReferences[connection].transform.rotation);

                //Notify other clients
                foreach (var c in m_Connections)
                {
                    if (c != connection && c.IsCreated) SendCharacterInfo(c, m_playerReferences[connection]);
                }

                break;
            case 0x07: //client ha fet habilitat
                break;
            default:
                Debug.Log("Unknown message type.");
                break;
        }
    }

    void CharacterSelectionResponse(NetworkConnection connection, PlayerReference player)
    {
        m_Driver.BeginSend(m_Pipeline, connection, out var writer);
        var messageType = (byte)(player == null ? 0x04 : 0x03);
        writer.WriteByte(messageType);

        if (messageType == 0x03)
        {
            writer.WriteFixedString128(player.character);
            writer.WriteFloat(player.transform.position.x);
            writer.WriteFloat(player.transform.position.y);
        }

        m_Driver.EndSend(writer);
    }

    void SendCharacterInfo(NetworkConnection connection, PlayerReference player)
    {
        m_Driver.BeginSend(m_Pipeline, connection, out var writer);
        writer.WriteByte(0x02);

        writer.WriteFixedString128(player.character);
        writer.WriteFloat(player.transform.position.x);
        writer.WriteFloat(player.transform.position.y);
        m_Driver.EndSend(writer);
    }


    void SendCorrectPositionToClients(NetworkConnection connection, Vector2 correctPosition)
    {
        m_Driver.BeginSend(m_Pipeline, connection, out var writer);
        writer.WriteByte(0x05);

        writer.WriteFloat(correctPosition.x);
        writer.WriteFloat(correctPosition.y);
        m_Driver.EndSend(writer);
    }
}