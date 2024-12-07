using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using UnityEditor.PackageManager;
using UnityEngine.SceneManagement;
using UnityEditor.MemoryProfiler;
using System.Collections.Generic;


public class ClientBehaviour : PersistentSingleton<ClientBehaviour>
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
    }

    public void Connectar()
    {
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
                SceneManager.LoadScene("ChoosePlayerScene");
                PayloadDeconstructor(stream);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Desconexión del servidor.");
                m_Connection = default;
            }
        }
    }

    public void EscollirPersonatge(int indexPersonatge)
    {
        m_Driver.BeginSend(m_Pipeline, m_Connection, out var writer);
        writer.WriteByte(0x02);
        writer.WriteFixedString128("Personaje" + indexPersonatge);
        m_Driver.EndSend(writer);
    }

    void PayloadDeconstructor(DataStreamReader stream)
    {
        byte messageType = stream.ReadByte();
        switch (messageType)
        {
            case 0x01: // Llista de personatges disponibles
                List<string> personatgesDisponibles = new List<string>();
                int nPersonatgesDisponibles = stream.ReadInt();

                for (int i = 0; i < nPersonatgesDisponibles; i++)
                {
                    string personatge = stream.ReadFixedString128().ToString();
                    personatgesDisponibles.Add(personatge);
                }

                Debug.Log(personatgesDisponibles);

                break;

            default:
                Debug.Log($"Unknown message type: {messageType}");
                break;
        }
    }

}
