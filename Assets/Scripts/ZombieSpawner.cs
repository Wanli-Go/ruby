using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    public GameObject zombiePrefab; // Assign this in the inspector
    public static int numberOfZombies = 3;
    public Vector2 mapCenter = Vector2.zero;

    public float mapWidth = 50f;
    public float mapHeight = 40f;
    public float spawnMargin = 5f; // Additional margin to ensure spawning outside the map

    public static GameObject[] zombies;

    void Start()
    {
        // Instantiate zombies and store references to them
        zombies = new GameObject[numberOfZombies];
        for (int i = 0; i < numberOfZombies; i++)
        {
            Vector2 spawnPosition = GenerateSpawnPosition();
            zombies[i] = Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);
            zombies[i].GetComponent<EnemyController>().Id = i;
            zombies[i].GetComponent<EnemyController>().targetPosition = mapCenter;
        }
    }

    Vector2 GenerateSpawnPosition()
    {
        while (true)
        {
            float x = Random.Range(-mapWidth / 2 - spawnMargin, mapWidth / 2 + spawnMargin);
            float y = Random.Range(-mapHeight / 2 - spawnMargin, mapHeight / 2 + spawnMargin);

            // Check if the position is outside the map
            if (x < -mapWidth / 2 || x > mapWidth / 2 || y < -mapHeight / 2 || y > mapHeight / 2)
                return new Vector2(x, y);
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            Vector2 target = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            foreach (GameObject zombie in zombies)
            {
                zombie.GetComponent<EnemyController>().targetPosition = target;
            }
        }
    }
}
