using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;

namespace Unity.Networking.Transport.Samples
{
    public class ServerBehaviour : MonoBehaviour
    {
        NetworkDriver m_Driver;
        NetworkPipeline m_Pipeline;
        NativeList<NetworkConnection> m_Connections;
        string serverName = "Sergi,Pol,Ramon SERVER";
        float startTime;

        void Start()
        {
            m_Driver = NetworkDriver.Create();
            m_Pipeline = m_Driver.CreatePipeline(
                typeof(ReliableSequencedPipelineStage),
                typeof(UnreliableSequencedPipelineStage)
            );
            m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

            var endpoint = NetworkEndpoint.AnyIpv4.WithPort(7777);
            if (m_Driver.Bind(endpoint) != 0)
            {
                Debug.LogError("Error al vincular el servidor al puerto 7777.");
                return;
            }
            m_Driver.Listen();
            startTime = Time.time;

            Debug.Log("Servidor iniciado y escuchando en el puerto 7777.");
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
                //Envia informacion al cliente dependiendo si es el primero o no
                PayloadConstructor(c, (byte)(m_Connections.Length == 1 ? 0x00 : 0x01), m_Connections.Length - 1);
            }
        }


        void PayloadConstructor(NetworkConnection connection, byte messageType, int clientIndex)
        {
            m_Driver.BeginSend(m_Pipeline, connection, out var writer);
            writer.WriteByte(messageType);

            switch (messageType)
            {
                case 0x00:
                    writer.WriteFixedString128(serverName);
                    writer.WriteFixedString128($"Cliente{clientIndex + 1}"); // Nombre del cliente
                    writer.WriteFloat(Time.time - startTime);
                    break;
                case 0x01:
                    writer.WriteFixedString128(serverName);
                    writer.WriteFixedString128($"Cliente{clientIndex + 1}"); // Nombre del cliente
                    writer.WriteFixedString128($"Cliente{clientIndex}"); // Nombre del cliente anterior
                    writer.WriteFloat(Time.time - startTime);
                    break;
            }

            m_Driver.EndSend(writer);
            Debug.Log($"Información enviada al Cliente{clientIndex + 1} utilizando un pipeline.");
        }
    }
}
