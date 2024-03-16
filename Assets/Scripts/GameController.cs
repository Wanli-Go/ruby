using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }
    public GameObject rubyPrefab;
    public GameObject zombiePrefab;
    public GameObject deathSentence; // UI for displaying ruby death message
    public GameObject winSentence; // UI for displaying ruby win message
    public GameObject lostSentence; // UI for displaying robot defeat message
    public GameObject killedSentence; // UI for displaying robot win message
    public ZombieSpawner spawner;
    public Transform canvas;

    private GameObject EnemyRuby = null; // Currently only one ruby is supported; used for destroying the gameobject

    private int activeZombies;

    private static Dictionary<int, GameObject> targetInstances = new();
    private static Dictionary<int, GameObject> zombieInstances = new();
    private static void RegisterInstances(int id, GameObject target)
    {
        if (!targetInstances.ContainsKey(id))
        {
            targetInstances[id] = target;
        }
    }
    private static void registerZombie(int id, GameObject target)
    {
        if (!zombieInstances.ContainsKey(id))
        {
            zombieInstances[id] = target;
        }
    }

    private static GameObject FindById(int id)
    {
        if (targetInstances.TryGetValue(id, out GameObject target))
        {
            return target;
        }

        return null;
    }

    private static GameObject FindZombieById(int id)
    {
        if (zombieInstances.TryGetValue(id, out GameObject target))
        {
            return target;
        }

        return null;
    }

    public static int PlayAsID = -1;

    private bool playSet = false;

    void Start()
    {
        Application.runInBackground = true;
        Application.targetFrameRate = 30;

        Instance = this;

        Client.Instance.OnRubyRoleSelected += InstantiateRuby;
        Client.Instance.OnRobotRoleSelected += InstantiateRobot;
        Client.Instance.OnRubyMovementReceived += UpdatePosition;
        Client.Instance.OnRobotFixed += FixRobot;
        Client.Instance.OnHealthChanged += UpdateHealth;

        activeZombies = ZombieSpawner.numberOfZombies;
    }
    private void UpdateHealth(int id, int health){
        if(health < 0){
            Instantiate(killedSentence, canvas);
            Destroy(EnemyRuby);
            return;
        }
        UIHealthBar.instance.SetValue(health/(float)RubyController.maxHealth);
    }

    // Robots player receives zombies fixed message
    private void FixRobot(int robotId)
    {
        EnemyController controller;
        for (int i = 0; i < ZombieSpawner.numberOfZombies; i++)
        {
            controller = ZombieSpawner.zombies[i].GetComponent<EnemyController>();
            // Change the pace of zombies for a higher win chance
            controller.speed += 1.0f;
            if (robotId != controller.Id) continue;
            
            controller.Fix();
            activeZombies--;
            if (activeZombies == 0)
            {
                Instantiate(lostSentence, canvas);
            }
        }
    }

    // The server can send positions about other players (not implemented) and and zombies.
    private void UpdatePosition(int id, Vector2 newPosition)
    {
        //Debug.Log("Position Updated!");
        GameObject target;
        if (id < 30) target = FindById(id);
        else target = FindZombieById(id);
        if (target == null && playSet == true) // Instantiate other player after setting local ID, and
        {
            if (id < 30)
            {
                InstantiateRuby(id);
                Debug.Log("Registered Ruby! with ID: " + id);
                return;
            }
            else
            {
                InstantiateZombies(id);
                Debug.Log("Registered Zombie! with ID: " + id);
                return;
            }
        }
        bool isRuby;
        AnimatedGameObject ruby = null;
        if (id < 30)
        {
            ruby = target.GetComponent<RubyController>();
            isRuby = true;
        }
        else
        {
            ruby = target.GetComponent<EnemyController>();
            if (target.GetComponent<EnemyController>().broken == false) return;
            isRuby = false;
        }
        if (ruby != null)
        {
            Vector2 oldPosition = ruby.transform.position;
            Vector2 moveDirection = newPosition - oldPosition;

            // Update position

            Animator animator = ruby.animator;

            // Update animation if Ruby is moving
            if (!(Math.Abs(moveDirection.x) < 0.02f) || !(Math.Abs(moveDirection.y) < 0.02f))
            {
                moveDirection.Normalize();

                // Set animation parameters
                animator.SetFloat("Look X", moveDirection.x);
                animator.SetFloat("Look Y", moveDirection.y);
                if (isRuby) animator.SetFloat("Speed", moveDirection.magnitude);
            }
            else
            {
                if (isRuby) animator.SetFloat("Speed", 0);
            }
        }
    }


    private void InstantiateRuby(int id)
    {
        GameObject ruby = Instantiate(rubyPrefab, new Vector3(-3.6f, -5.4f, 0), Quaternion.identity);
        EnemyRuby = ruby;
        ruby.GetComponent<RubyController>().Id = id;
        if (ChooserUI.PlayAs && !playSet)
        {
            PlayAsID = id;
            playSet = true;
        }
        RegisterInstances(id, ruby);
    }

    private void InstantiateZombies(int id)
    {
        GameObject zombie = Instantiate(zombiePrefab, new Vector3(50f, 50f, 0), Quaternion.identity);
        zombie.GetComponent<EnemyController>().Id = id;
        registerZombie(id, zombie);
    }


    private void InstantiateRobot(int id)
    {
        Instantiate(spawner);
        CinemachineVirtualCamera cam = FindObjectOfType<CinemachineVirtualCamera>();
        cam.transform.position = new Vector3(0, 0, -20);
        cam.m_Lens.OrthographicSize = 12;
        PlayAsID = id;
        if (!ChooserUI.PlayAs && !playSet)
        {
            PlayAsID = id;
            playSet = true;
        }
    }

    public void RubyDeath()
    {
        Instantiate(deathSentence, canvas);
    }

    public void ZombiesHit(){
        activeZombies -= 1;
        if(activeZombies == 0){
            Instantiate(winSentence, canvas);
        }
    }

    void OnDestroy()
    {
        if (Client.Instance != null)
        {
            Client.Instance.OnRubyRoleSelected -= InstantiateRuby;
        }
    }
}

