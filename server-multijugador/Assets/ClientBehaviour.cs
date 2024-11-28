using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;

namespace Unity.Networking.Transport.Samples
{
    public class ClientBehaviour : MonoBehaviour
    {
        NetworkDriver m_Driver;
        NetworkPipeline m_Pipeline;
        NetworkConnection m_Connection;
        public string serverIP = "127.0.0.1";
        public ushort serverPort = 7777;
        private bool hasSentMessage = false;

        void Start()
        {
            m_Driver = NetworkDriver.Create();
            m_Pipeline = m_Driver.CreatePipeline( // Configura el pipeline
                typeof(ReliableSequencedPipelineStage),
                typeof(UnreliableSequencedPipelineStage)
            );

            var endpoint = NetworkEndpoint.Parse(serverIP, serverPort);
            m_Connection = m_Driver.Connect(endpoint);

            Debug.Log($"Intentando conectar al servidor en {serverIP}:{serverPort}");
        }

        void OnDestroy()
        {
            m_Driver.Dispose();
        }

        void Update()
        {
            m_Driver.ScheduleUpdate().Complete();

            if (!m_Connection.IsCreated)
            {
                return;
            }

            Unity.Collections.DataStreamReader stream;
            NetworkEvent.Type cmd;
            while ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Connect)
                {
                    Debug.Log("Conexión establecida con el servidor.");
                    EnviarMensaje();
                }
                else if (cmd == NetworkEvent.Type.Data)
                {
                    byte messageType = stream.ReadByte();
                    string serverName = stream.ReadFixedString128().ToString();
                    string clientName = stream.ReadFixedString128().ToString();
                    string previousClient = stream.ReadFixedString128().ToString();
                    float serverTime = stream.ReadFloat();

                    Debug.Log($"Tipo de Mensaje: {messageType}, Servidor: {serverName}, Cliente: {clientName}, Cliente Anterior: {previousClient}, Tiempo del Servidor: {serverTime}");
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Desconexión del servidor.");
                    m_Connection = default;
                }
            }
        }

        void EnviarMensaje()
        {
            if (hasSentMessage) return;

            Debug.Log("Enviando mensaje al servidor usando el pipeline...");
            m_Driver.BeginSend(m_Pipeline, m_Connection, out var writer);
            writer.WriteByte(0x02); // Identificador de mensaje
            writer.WriteFixedString128("Cliente Personalizado"); // Nombre del cliente
            m_Driver.EndSend(writer);

            hasSentMessage = true; // Evitar enviar múltiples veces el mismo mensaje
        }
    }
}
