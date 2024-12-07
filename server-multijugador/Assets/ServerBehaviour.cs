using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using System.Collections.Generic;

namespace Unity.Networking.Transport.Samples
{
    public class ServerBehaviour : MonoBehaviour
    {
        NetworkDriver m_Driver;
        NetworkPipeline m_Pipeline;
        NativeList<NetworkConnection> m_Connections;
        string serverName = "Sergi, Pol, Ramon SERVER";
        List<string> availableCharacters = new List<string> { "Personaje1", "Personaje2", "Personaje3" };
        Dictionary<NetworkConnection, string> selectedCharacters = new Dictionary<NetworkConnection, string>();

        private readonly ushort Port = 7777;

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
            Debug.Log($"Servidor iniciado en la IP: {NetworkEndpoint.AnyIpv4.Address} y puerto: {Port}");
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

            DataStreamReader stream;
            for (int i = 0; i < m_Connections.Length; i++)
            {
                if (m_Connections[i].IsCreated)
                {
                    NetworkEvent.Type cmd;
                    while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) != NetworkEvent.Type.Empty)
                    {
                        if (cmd == NetworkEvent.Type.Data)
                        {
                            ProcessClientMessage(m_Connections[i], stream);
                        }
                    }
                }
            }
        }

        void SendAvailableCharacters(NetworkConnection connection)
        {
            m_Driver.BeginSend(m_Pipeline, connection, out var writer);
            writer.WriteByte(0x01); // Tipo de mensaje: Lista de personajes disponibles
            writer.WriteInt(availableCharacters.Count);

            foreach (var character in availableCharacters)
            {
                writer.WriteFixedString128(character);
            }
            m_Driver.EndSend(writer);
            Debug.Log("Lista de personajes enviados.");
        }

        void ProcessClientMessage(NetworkConnection connection, DataStreamReader stream)
        {
            byte messageType = stream.ReadByte();
            if (messageType == 0x02) // Selección de personaje
            {
                var selectedCharacter = stream.ReadFixedString128().ToString();
                if (availableCharacters.Contains(selectedCharacter))
                {
                    availableCharacters.Remove(selectedCharacter);
                    selectedCharacters[connection] = selectedCharacter;
                    Debug.Log($"Cliente seleccionó: {selectedCharacter}");
                    ConfirmCharacterSelection(connection, selectedCharacter);
                }
                else
                {
                    NotifyCharacterUnavailable(connection);
                }
            }
        }

        void ConfirmCharacterSelection(NetworkConnection connection, string character)
        {
            m_Driver.BeginSend(m_Pipeline, connection, out var writer);
            writer.WriteByte(0x03); // Confirmación de personaje
            writer.WriteFixedString128(character);
            m_Driver.EndSend(writer);
        }

        void NotifyCharacterUnavailable(NetworkConnection connection)
        {
            m_Driver.BeginSend(m_Pipeline, connection, out var writer);
            writer.WriteByte(0x04); // Notificación de personaje no disponible
            m_Driver.EndSend(writer);
        }
    }
}
