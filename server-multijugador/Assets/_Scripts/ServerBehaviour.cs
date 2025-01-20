using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using System.Collections.Generic;
using TMPro;
using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using UnityEditor.MemoryProfiler;

[Serializable]
public class PlayerReference
{
    public string character;
    public Transform transform;
    public Vector2 lastRecievedVelocity;
    public SimulatedPlayerBehaviour behaviour;
}

public class ServerBehaviour : StaticSingleton<ServerBehaviour>
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
        };

    [SerializeField, ReadOnly] private List<string> m_availableCharacters;
    [SerializeField] List<PlayerReference> m_initialPlayerReferences = new List<PlayerReference>();
    [SerializeField] public float moveDistanceThreshold = 1f;

    [SerializeField] List<EnemyBehaviour> m_enemies;
    [SerializeField] private float positionThreshold = 0.05f; // Minimum distance to trigger an update instantly
    [SerializeField] private float updateInterval = 0.1f; // Time in seconds between updates
    private float updateTimer;

    [SerializeField] GameObject m_projectilePrefab;

    [SerializeField] private Transform enemyTransform; // Transform del enemigo
    [SerializeField] private float enemySpeed = 2.0f;   // Velocidad del enemigo
    private Vector2 enemyDirection = Vector2.right;    // Dirección inicial del movimiento

    Dictionary<NetworkConnection, PlayerReference> m_playerReferences = new Dictionary<NetworkConnection, PlayerReference>();

    public int GetCharacterIndexByName(string name) => m_characters.FindIndex(c => c.name == name);

    private new void Awake()
    {
        base.Awake();
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

        updateTimer = 0f;

        for (int i = 0; i < m_enemies.Count; i++) m_enemies[i].ID = i; // Setejem les IDs dels enemics
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

    private void HandleClientDisconnect(NetworkConnection connection)
    {
        if (m_playerReferences.TryGetValue(connection, out var playerReference))
        {
            m_playerReferences.Remove(connection);
            m_availableCharacters.Add(playerReference.character);

            foreach (var conn in m_Connections)
            {
                if (conn != connection && conn.IsCreated)
                {
                    //NotifyPlayerDisconnect(conn, playerReference.character);
                }
            }

            Debug.Log($"Player {playerReference.character} disconnected.");
        }
    }

    private int connectedClients = 0;
    private bool readyToStart = false;
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
            connectedClients++;
            if (connectedClients == 2 && !readyToStart)
            {
                readyToStart = true;
                NotifyStartToAllClients();
            }
            else if (connectedClients < 2)
            {
                SendWaitScene(c);
            }
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
                        HandleClientDisconnect(m_Connections[i]);
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

        updateTimer = Mathf.Max(0f, updateTimer - Time.deltaTime);

        if (updateTimer <= 0f) // Update the positions of all the enemies' positions if timer has elapsed
        {
            SendEnemyPositions();
        }
    }

    void SendWaitScene(NetworkConnection connection)
    {
        m_Driver.BeginSend(m_Pipeline, connection, out var writer);
        writer.WriteByte(0x14);
        m_Driver.EndSend(writer);
    }
    void NotifyStartToAllClients()
    {
        foreach (var connection in m_Connections)
        {
            if (connection.IsCreated)
            {
                m_Driver.BeginSend(m_Pipeline, connection, out var writer);
                writer.WriteByte(0x15);
                m_Driver.EndSend(writer);
            }
        }
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
                    transform = m_initialPlayerReferences[m_Connections.IndexOf(connection)].transform,
                    behaviour = m_initialPlayerReferences[m_Connections.IndexOf(connection)].behaviour,
                    lastRecievedVelocity = Vector2.zero
                };
                m_playerReferences[connection] = playerReference;


                Debug.Log($"Cliente seleccionó: {selectedCharacter}");
                CharacterSelectionResponse(connection, m_playerReferences[connection]);


                //Setup character in server scene
                m_playerReferences[connection].behaviour.SetupCharacter(m_characters[GetCharacterIndexByName(selectedCharacter)]);

                //Notify other clients
                foreach (var c in m_Connections)
                {
                    if (c != connection && c.IsCreated) SendCharacterInfo(c, m_playerReferences[connection]);
                }

                break;

            case 0x06:
                var x = stream.ReadFloat();
                var y = stream.ReadFloat();
                var vx = stream.ReadFloat();
                var vy = stream.ReadFloat();

                Vector2 newPos = new Vector2(x, y);
                Vector2 newVel = new Vector2(vx, vy);
                Vector2 lastPos = m_playerReferences[connection].transform.position;


                if (Vector2.Distance(newPos, lastPos) > moveDistanceThreshold)
                {
                    Vector2 directionVec = (newPos - lastPos).normalized;
                    newPos = lastPos + directionVec * moveDistanceThreshold; //Capem el moviment al max threshold 

                    SendCorrectPositionToClients(connection, newPos);
                }

                m_playerReferences[connection].transform.SetPositionAndRotation(newPos, m_playerReferences[connection].transform.rotation);
                m_playerReferences[connection].lastRecievedVelocity = newVel;

                //Notify other clients
                foreach (var c in m_Connections)
                {
                    if (c != connection && c.IsCreated) SendCharacterInfo(c, m_playerReferences[connection]);
                }

                break;
            case 0x07: //client ha fet habilitat
                var xPos = stream.ReadFloat();
                var yPos = stream.ReadFloat();
                var direction = stream.ReadFloat(); //-1 left 1 right
                m_playerReferences[connection].behaviour.ActivateAbility(new Vector2(xPos, yPos), direction);

                //Notify other clients
                foreach (var c in m_Connections)
                {
                    if (c != connection && c.IsCreated) AbilityActivationResponse(c, m_playerReferences[connection].character, new Vector2(xPos, yPos), direction);
                }
                break;

            default:
                Debug.Log("Unknown message type.");
                break;
        }
    }

    void SendAvailableCharacters(NetworkConnection connection) //0x00
    {
        m_Driver.BeginSend(m_Pipeline, connection, out var writer);
        writer.WriteByte(0x00); // Tipo de mensaje: Lista de personajes disponibles
        writer.WriteInt(m_availableCharacters.Count);

        foreach (var character in m_availableCharacters)
        {
            writer.WriteFixedString128(character);

            //enviem tambe l'abilitat, aixi el servidor es l'unic que te la veritat de quin personatge te cada abilitat
            int characterIndex = GetCharacterIndexByName(character);
            int ability = (int)m_characters[characterIndex].ability;
            writer.WriteInt(ability);
        }
        m_Driver.EndSend(writer);
        Debug.Log("Lista de personajes enviados.");
    }

    void SendCharacterInfo(NetworkConnection connection, PlayerReference player) //0x02
    {
        m_Driver.BeginSend(m_Pipeline, connection, out var writer);
        writer.WriteByte(0x02);

        writer.WriteFixedString128(player.character);
        writer.WriteFloat(player.transform.position.x);
        writer.WriteFloat(player.transform.position.y);
        writer.WriteFloat(player.lastRecievedVelocity.x);
        writer.WriteFloat(player.lastRecievedVelocity.y);
        m_Driver.EndSend(writer);
    }

    void CharacterSelectionResponse(NetworkConnection connection, PlayerReference player) //0x03 & 0x04
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

    void SendCorrectPositionToClients(NetworkConnection connection, Vector2 correctPosition) //0x05
    {
        m_Driver.BeginSend(m_Pipeline, connection, out var writer);
        writer.WriteByte(0x05);

        writer.WriteFloat(correctPosition.x);
        writer.WriteFloat(correctPosition.y);
        m_Driver.EndSend(writer);
    }

    void AbilityActivationResponse(NetworkConnection connection, string character, Vector2 position, float direction) //0x08
    {
        m_Driver.BeginSend(m_Pipeline, connection, out var writer);
        writer.WriteByte(0x08);
        writer.WriteFixedString128(character);
        writer.WriteFloat(position.x);
        writer.WriteFloat(position.y);
        writer.WriteFloat(direction);
        m_Driver.EndSend(writer);
    }

    public void SendEnemyPositions() //0x09
    {
        foreach (var conn in m_Connections)
        {
            foreach (var enemy in m_enemies)
            {
                Vector2 enemyPos = enemy.GetActualPosition();

                m_Driver.BeginSend(m_Pipeline, conn, out var writer);
                writer.WriteByte(0x09); // Tipo de mensaje: posición del enemigo
                writer.WriteInt(enemy.ID);
                writer.WriteFloat(enemyPos.x);
                writer.WriteFloat(enemyPos.y);
                m_Driver.EndSend(writer);
            }

            updateTimer = updateInterval;
        }
    }


    public void NotifyEnemyHit(GameObject enemy) //0x10
    {
        foreach (var connection in m_Connections)
        {
            m_Driver.BeginSend(m_Pipeline, connection, out var writer);
            writer.WriteByte(0x10);
            //TODO : Chequear en que indice esta el enemigo int enemyIndex = enemyList.IndexOf(enemy);
            int enemyIndex = 0;

            writer.WriteInt(enemyIndex);
            m_Driver.EndSend(writer);
        }
    }

    public void OnPlayerDamaged(string character) //0x11
    {
        foreach (var conn in m_Connections)
        {
            m_Driver.BeginSend(m_Pipeline, conn, out var writer);
            writer.WriteByte(0x11); // Message type for collision
            writer.WriteFixedString128(character);
            m_Driver.EndSend(writer);
        }
    }


    public void NotifyProjectileDeletion() //0x12
    {
        foreach (var connection in m_Connections)
        {
            m_Driver.BeginSend(m_Pipeline, connection, out var writer);
            writer.WriteByte(0x12);
            m_Driver.EndSend(writer);
        }
    }

    public void NotifyPlayerFinish()//0x13
    {
        foreach (var connection in m_Connections)
        {
            m_Driver.BeginSend(m_Pipeline, connection, out var writer);
            writer.WriteByte(0x13); // Message type for player finish
            m_Driver.EndSend(writer);
        }
    }

    public void CreateProjectile(Vector2 position, float direction)
    {
        GameObject projectile = Instantiate(m_projectilePrefab, position, Quaternion.identity);
        projectile.GetComponent<Rigidbody2D>().AddForce(new Vector2(direction, 0) * 15, ForceMode2D.Impulse);
        if (direction < 0) projectile.GetComponent<SpriteRenderer>().flipX = true;
    }
}