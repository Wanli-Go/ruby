using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Timers;

using UnityEngine;
public class EnemyController : AnimatedGameObject
{
    public int Id { get; set; }
    public float speed = 3.0f;
    public Vector2 targetPosition; // Target position for the enemy to move towards




    public bool broken = true;

    // Start is called before the first frame update
    void Start()
    {
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

    }


    void Update()
    {
        if (Id > 30) return;
        if (!broken) return;

        // Optional: Add additional update logic here if needed
    }

    System.Random random = new System.Random();
    double minValue = 0.02;
    double maxValue = 0.05;

    double messageTimer;
    bool isOnCooldown = false;

    void FixedUpdate()
    {
        if (Id >= 30) return;
        if (!broken) return;

        Vector2 position = body.position;

        // Calculate the direction towards the target position
        Vector2 direction = (targetPosition - position).normalized;

        // Move towards the target position
        position += direction * speed * Time.deltaTime;

        // Set the animation parameters based on direction
        animator.SetFloat("Look X", direction.x);
        animator.SetFloat("Look Y", direction.y);

        body.MovePosition(position);
        if (isOnCooldown == false)
        {
            byte[] message = CreateMovementMessage(position);
            Client.Instance.SendMessageToServer(message);
            isOnCooldown = true;
            messageTimer = minValue + (maxValue - minValue) * random.NextDouble();
        } else {
            messageTimer -= Time.deltaTime;
            if(messageTimer < 0) isOnCooldown = false;
        }
    }

    private byte[] CreateMovementMessage(Vector2 position)
    {
        using MemoryStream stream = new();
        using (BinaryWriter writer = new(stream))
        {
            // Message Type
            writer.Write((byte)0x02);

            // Placeholder for Message Length
            writer.Write(0);

            // Selected Role
            writer.Write(Id + 30);
            writer.Write(position.x);
            writer.Write(position.y);

            // Go back and write the correct message length
            long msgLen = stream.Length - 5;
            stream.Seek(1, SeekOrigin.Begin);
            writer.Write((int)msgLen);
        }

        return stream.ToArray();
    }



    void OnCollisionEnter2D(Collision2D other)
    {
        if (Id < 30) return; // Don't calculate health in robot controller
        RubyController player = other.gameObject.GetComponent<RubyController>();

        if (player != null && player.Id == GameController.PlayAsID)
        {
            player.ChangeHealth(-1);
        }
    }

    public void Fix()
    {
        broken = false;
        body.simulated = false;
        animator.SetTrigger("Fixed");
        if (Id >= 30)
        {
            byte[] message = CreateFixedMessage();
            Client.Instance.SendMessageToServer(message);
            GameController.Instance.ZombiesHit();
        }
    }
    private byte[] CreateFixedMessage()
    {
        using MemoryStream stream = new();
        using (BinaryWriter writer = new(stream))
        {
            // Message Type
            writer.Write((byte)0x03);

            // Placeholder for Message Length
            writer.Write(0);

            // Selected Role
            writer.Write(Id);

            // Go back and write the correct message length
            long msgLen = stream.Length - 5;
            stream.Seek(1, SeekOrigin.Begin);
            writer.Write((int)msgLen);
        }

        return stream.ToArray();
    }
}
