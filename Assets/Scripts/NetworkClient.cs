using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

public class Client : MonoBehaviour
{
    public static Client Instance { get; private set; }

    private TcpClient tcpClient;
    private NetworkStream stream;
    private BinaryWriter writer;
    private BinaryReader reader;

    public event Action<int> OnRubyRoleSelected;

    public event Action<int> OnRobotRoleSelected;

    public event Action<int, Vector2> OnRubyMovementReceived;

    public event Action<int> OnRobotFixed;
    public event Action<int, int> OnHealthChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ConnectToServer("127.0.0.1", 10035);
    }

    void ConnectToServer(string serverIP, int port)
    {
        try
        {
            tcpClient = new TcpClient(serverIP, port);
            stream = tcpClient.GetStream();
            writer = new BinaryWriter(stream);
            reader = new BinaryReader(stream);
        }
        catch (Exception e)
        {
            Debug.LogError("Socket error: " + e.Message);
        }
    }

    public void SendMessageToServer(byte[] message)
    {
        if (stream != null && stream.CanWrite)
        {
            writer.Write(message);
            writer.Flush();
        }
    }
    void Update()
    {
        if (stream != null && stream.CanRead)
        {
            // Check if there is data from the server
            if (stream.DataAvailable)
            {
                HandleServerResponse();
            }
        }
    }

    private void HandleServerResponse()
    {

        byte messageType = reader.ReadByte();

        if (messageType == 0x02) // Assuming 0x02 is movement message
        {
            //Debug.Log("Is Movement Message");
            int targetId = reader.ReadInt32();
            Vector2 position = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            OnRubyMovementReceived?.Invoke(targetId, position);
            return;
        }

        if (messageType == 0x03) // Assuming 0x03 is Fixed message
        {

            int fixedZombie = reader.ReadInt32();
            Debug.Log("Robot Fixed Message at ID:" + fixedZombie);
            OnRobotFixed?.Invoke(fixedZombie);
            return;
        }

        if (messageType == 0x04) // Assuming 0x04 is Health Change message
        {
            int healthId = reader.ReadInt32();
            int health = reader.ReadInt32();
            Debug.Log("Health Changed Message at ID: " + healthId + "Value: " + health);
            OnHealthChanged?.Invoke(healthId, health);
            return;
        }

        // messageType == 0x01 == select roles
        // Read the response from the server
        int length = reader.ReadInt32();
        int id = reader.ReadInt32();

        // Read the actual string
        string role = new(reader.ReadChars(length));

        if (role == "Ruby")
        {
            // Trigger the event to instantiate Ruby
            OnRubyRoleSelected?.Invoke(id);
            Debug.Log("Created Ruby");
        }

        if (role == "Robot")
        {
            OnRobotRoleSelected?.Invoke(id);
            Debug.Log("Created Robot");
        }
        Thread.Sleep(100);
    }


    void OnApplicationQuit()
    {
        tcpClient.Close();
        writer.Close();
        reader.Close();
        stream.Close();
    }
}

