using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{

    Rigidbody2D body;
    // Start is called before the first frame update
    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 direction, float force)
    {
        body.AddForce(direction * force);
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        // Debug.Log("Projectile Hit! w/ " + other.gameObject.ToString());
        EnemyController enemy = other.collider.GetComponent<EnemyController>();
        if (enemy)
        {
            enemy.Fix();
        }
        Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.position.magnitude > 20.0f)
        {
            Destroy(gameObject);
        }
    }
}
