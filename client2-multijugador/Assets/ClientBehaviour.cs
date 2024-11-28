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


            DataStreamReader stream;
            NetworkEvent.Type cmd;
            while ((cmd = m_Connection.PopEvent(m_Driver, out stream, out var pipeline)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Connect)
                {
                    Debug.Log("Conexión establecida con el servidor.");
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
        void PayloadDeconstructor(DataStreamReader stream)
        {
            byte messageType = stream.ReadByte();
            switch (messageType)
            {
                case 0x00:
                    string serverName = stream.ReadFixedString128().ToString();
                    string clientName = stream.ReadFixedString128().ToString();
                    float serverTime = stream.ReadFloat();

                    Debug.Log(
                        $"Message Type: 0x00\n" +
                        $"  Server:        {serverName}\n" +
                        $"  Client:        {clientName}\n" +
                        $"  Server Time:   {serverTime:F2} seconds"
                    );
                    break;

                case 0x01:
                    string serverName2 = stream.ReadFixedString128().ToString();
                    string clientName2 = stream.ReadFixedString128().ToString();
                    string previousClient = stream.ReadFixedString128().ToString();
                    float serverTime2 = stream.ReadFloat();

                    Debug.Log(
                        $"Message Type: 0x01\n" +
                        $"  Server:         {serverName2}\n" +
                        $"  Client:         {clientName2}\n" +
                        $"  Previous Client: {previousClient}\n" +
                        $"  Server Time:    {serverTime2:F2} seconds"
                    );
                    break;

                default:
                    Debug.Log($"Unknown message type: {messageType}");
                    break;
            }
        }

    }
}
