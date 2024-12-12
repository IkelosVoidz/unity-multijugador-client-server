using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using System.Collections.Generic;
using TMPro;
using System;
using System.Net;
using System.Net.Sockets;

namespace Unity.Networking.Transport.Samples
{
    public class ServerBehaviour : MonoBehaviour
    {
        [SerializeField] private TMP_Text IP_Text;

        NetworkDriver m_Driver;
        NetworkPipeline m_Pipeline;
        NativeList<NetworkConnection> m_Connections;
        [SerializeField] List<string> m_characters = new List<string> { "Personaje1", "Personaje2", "Personaje3", "Personaje4" };
        [SerializeField, ReadOnly] private List<string> m_availableCharacters;
        Dictionary<NetworkConnection, string> selectedCharacters = new Dictionary<NetworkConnection, string>();

        public struct Posicio
        {
            public float _x { get; set; }
            public float _y { get; set; }

            public Posicio(float x, float y)
            {
                _x = x;
                _y = y;
            }
        }

        public List<Posicio> initialPositions = new List<Posicio>() {new Posicio(1f, 1f), new Posicio(2f, 2f)};
        private Dictionary<NetworkConnection, Posicio> characterPositions = new Dictionary<NetworkConnection, Posicio>();

        private void Awake()
        {
            m_availableCharacters = new List<string>(m_characters);
        }

        [SerializeField] ushort Port = 7777;

        void Start()
        {
            m_Driver = NetworkDriver.Create();
            m_Pipeline = m_Driver.CreatePipeline(
                typeof(ReliableSequencedPipelineStage),
                typeof(UnreliableSequencedPipelineStage)
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
        }

        void SendAvailableCharacters(NetworkConnection connection)
        {
            m_Driver.BeginSend(m_Pipeline, connection, out var writer);
            writer.WriteByte(0x01); // Tipo de mensaje: Lista de personajes disponibles
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
                case 0x02: // Selección de personaje
                    string selectedCharacter = stream.ReadFixedString128().ToString();
                    if (!m_availableCharacters.Contains(selectedCharacter)) //Si ja esta seleccionat
                    {
                        Debug.Log("Cliente seleccionó un personaje ya seleccionado");
                        CharacterSelectionResponse(connection, null);
                        return;
                    }

                    m_availableCharacters.Remove(selectedCharacter);
                    selectedCharacters[connection] = selectedCharacter;
                    Debug.Log($"Cliente seleccionó: {selectedCharacter}");
                    CharacterSelectionResponse(connection, selectedCharacter);

                    //Notify other clients
                    foreach (var c in m_Connections)
                    {
                        if (c != connection && c.IsCreated) SendAvailableCharacters(c);
                    }
                    
                    break;
                
                case 0x06: // Cliente envia la posicion nueva a la que se quiere mover
                    //characterPositions[connection] = initialPositions[]
                    break;
                
            }
        }

        void CharacterSelectionResponse(NetworkConnection connection, string character)
        {
            m_Driver.BeginSend(m_Pipeline, connection, out var writer);
            var messageType = (byte)(character == null ? 0x04 : 0x03);
            writer.WriteByte(messageType);

            if (messageType == 0x03) {
                writer.WriteFixedString128(character);

            }

            m_Driver.EndSend(writer);
        }
    }
}
