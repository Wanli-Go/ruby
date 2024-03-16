using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;
using System;
using System.IO;

public class ChooserUI : MonoBehaviour
{
    [SerializeField] private Button RubyButton;
    [SerializeField] private Button ZombiesButton;

    public static bool PlayAs; // Ruby = true, Robots = false

    private void Awake()
    {
        RubyButton.onClick.AddListener(() =>
            ChooseRole("Ruby")
            );
        ZombiesButton.onClick.AddListener(() => ChooseRole("Zombies"));
    }

    private void ChooseRole(string role)
    {
        PlayAs = role == "Ruby" ? true : false;
        byte[] message = CreateRoleSelectionMessage(role);
        Client.Instance.SendMessageToServer(message);
        Debug.Log("Message Sent:" + message);
        Destroy(gameObject);
    }

    private byte[] CreateRoleSelectionMessage(string role)
    {
        using MemoryStream stream = new();
        using (BinaryWriter writer = new(stream))
        {
            // Message Type
            writer.Write((byte)0x01);

            // Placeholder for Message Length
            writer.Write(0);

            // Selected Role
            writer.Write(role == "Ruby" ? (byte)'R' : (byte)'Z');

            // Go back and write the correct message length
            long msgLen = stream.Length - 5; // Type + Length
            stream.Seek(1, SeekOrigin.Begin);
            writer.Write((int)msgLen);
        }

        return stream.ToArray();
    }

}

