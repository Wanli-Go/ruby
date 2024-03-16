using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Unity.Netcode;
using System.IO;
using System;


public class RubyController : AnimatedGameObject
{
    public int Id { get; set; }
    public bool IsPlayer { get; set; }

    public static int maxHealth = 5;
    float speed = 5.0f;
    public float invicibleTime = 1.0f;
    public float fireCooldown = 1.2f;

    int currentHealth;
    public int health
    {
        get
        {
            return currentHealth;
        }
    }

    bool isInvincible;
    float invicibleTimer;

    bool isInFireCooldown;
    float fireCooldownTimer;
    float horizontalInput;
    float verticalInput;
    Vector2 lookDirection = new Vector2(1, 0);


    public CinemachineVirtualCamera virtualCamera;

    void Start()
    {
        body = GetComponent<Rigidbody2D>();

        currentHealth = maxHealth;
        isInvincible = false;

        isInFireCooldown = false;

        animator = GetComponent<Animator>();

        if (Id == GameController.PlayAsID)
        {
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();

            virtualCamera.Follow = transform;
        }
    }



    void Update()
    {
        if (GameController.PlayAsID != Id) return;
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");


        Vector2 move = new Vector2(horizontalInput, verticalInput);

        if (!Mathf.Approximately(move.x, 0.0f) || !Mathf.Approximately(move.y, 0.0f))
        {
            lookDirection.Set(move.x, move.y);
            lookDirection.Normalize();
        }

        animator.SetFloat("Look X", lookDirection.x);
        animator.SetFloat("Look Y", lookDirection.y);
        animator.SetFloat("Speed", move.magnitude);

        // Debug.Log(horizontal);
        if (isInvincible)
        {
            invicibleTimer -= Time.deltaTime;
            if (invicibleTimer < 0)
            {
                isInvincible = false;
            }
        }

        if (Input.GetButtonDown("Fire1"))
        {
            if (!isInFireCooldown)
            {
                isInFireCooldown = true;
                fireCooldownTimer = fireCooldown;
                Debug.Log("Clicked");
                Launch();
            }
        }

        if (isInFireCooldown)
        {
            fireCooldownTimer -= Time.deltaTime;
            if (fireCooldownTimer < 0) isInFireCooldown = false;
        }
    }
    System.Random random = new System.Random();
    double minValue = 0.015;
    double maxValue = 0.03;

    double messageTimer;
    bool isOnCooldown = false;
    void FixedUpdate()
    {
        if (GameController.PlayAsID != Id) return;
        Vector2 position = transform.position;
        position.x = position.x + speed * horizontalInput * Time.deltaTime;
        position.y = position.y + speed * verticalInput * Time.deltaTime;
        body.MovePosition(position);

        if (GameController.PlayAsID == Id)
        {
            if (isOnCooldown == false)
            {
                byte[] message = CreateMovementMessage(position);
                Client.Instance.SendMessageToServer(message);
                isOnCooldown = true;
                messageTimer = minValue + (maxValue - minValue) * random.NextDouble();
            }
            else
            {
                messageTimer -= Time.deltaTime;
                if (messageTimer < 0) isOnCooldown = false;
            }
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

            writer.Write(Id);

            // Selected Role
            writer.Write(position.x);
            writer.Write(position.y);

            // Go back and write the correct message length
            long msgLen = stream.Length - 5;
            stream.Seek(1, SeekOrigin.Begin);
            writer.Write((int)msgLen);
        }

        return stream.ToArray();
    }



    public void ChangeHealth(int amount)
    {
        if (amount < 0)
        {
            if (isInvincible) return;

            isInvincible = true;
            invicibleTimer = invicibleTime;
            animator.SetTrigger("Hit");
        }

        currentHealth = Mathf.Clamp(currentHealth + amount, -1, maxHealth);
        UIHealthBar.instance.SetValue(currentHealth / (float)maxHealth);

        if (GameController.PlayAsID == Id)
        {
            byte[] message = CreateHealthChangedMessage(currentHealth);
            Client.Instance.SendMessageToServer(message);
        }

        if (currentHealth < 0)
        {
            Destroy(gameObject);
            GameController.Instance.RubyDeath();
        }
    }
    private byte[] CreateHealthChangedMessage(int health)
    {
        using MemoryStream stream = new();
        using (BinaryWriter writer = new(stream))
        {
            // Message Type
            writer.Write((byte)0x04);

            // Placeholder for Message Length
            writer.Write(0);

            writer.Write(Id);

            // Selected Role
            writer.Write(currentHealth);

            // Go back and write the correct message length
            long msgLen = stream.Length - 5;
            stream.Seek(1, SeekOrigin.Begin);
            writer.Write((int)msgLen);
        }

        return stream.ToArray();
    }


    public GameObject projectilePrefab;

    void Launch()
    {
        GameObject projectileObject =
            Instantiate(projectilePrefab, body.position + Vector2.up * 0.5f, Quaternion.identity);

        Projectile projectile = projectileObject.GetComponent<Projectile>();
        projectile.Launch(lookDirection, 300);

        animator.SetTrigger("Launch");
    }


}
