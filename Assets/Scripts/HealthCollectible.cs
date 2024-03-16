using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HealthCollectible : MonoBehaviour
{
    RubyController ruby;
    void OnTriggerEnter2D(Collider2D other){

        ruby = other.GetComponent<RubyController>();
        if (ruby == null) return;

        if(ruby.health == RubyController.maxHealth) return;
        ruby.ChangeHealth(1);
        Destroy(gameObject);
        
        Debug.Log("Triggered Health Collectible Collide");

    }
}
