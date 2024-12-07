using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using UnityEditor.PackageManager;
using UnityEngine.SceneManagement;
using UnityEditor.MemoryProfiler;
using System.Collections.Generic;
using TMPro;
using System;


public class ClientBehaviour : PersistentSingleton<ClientBehaviour>
{
    NetworkDriver m_Driver;
    NetworkPipeline m_Pipeline;
    NetworkConnection m_Connection;
    public TMP_InputField serverIP;
    public TMP_InputField serverPort;

    private string CharacterChosen = null;
    private bool IsCharacterChosenConfirmed = false;

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
        var endpoint = NetworkEndpoint.Parse(serverIP.text, Convert.ToUInt16(serverPort.text));
        m_Connection = m_Driver.Connect(endpoint);

        Debug.Log($"Intentando conectar al servidor en {serverIP.text}:{serverPort.text}");
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
        CharacterChosen = "Personaje" + indexPersonatge;

        m_Driver.BeginSend(m_Pipeline, m_Connection, out var writer);
        writer.WriteByte(0x02);
        writer.WriteFixedString128(CharacterChosen);
        m_Driver.EndSend(writer);
    }

    public string GetPersonatgeEscollit()
    {
        if (IsCharacterChosenConfirmed) return CharacterChosen;

        return null;
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
                    Debug.Log(personatge);
                }

                break;

            case 0x03: // El personaje ha ido escogido correctamente
                IsCharacterChosenConfirmed = true;
                Debug.Log("El personatge s'ha escollit correctament");

                SceneManager.LoadScene("CharacterChosen");

                break;

            case 0x04: // El personaje ha ido escogido por otra conexion.
                CharacterChosen = "";
                IsCharacterChosenConfirmed = false;

                Debug.Log("El personatge ha estat escollit per una altra connexió");
                break;

            default:
                Debug.Log($"Unknown message type: {messageType}");
                break;
        }
    }

}
