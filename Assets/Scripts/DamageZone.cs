using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageZone : MonoBehaviour
{
    RubyController ruby;

    void OnTriggerStay2D(Collider2D other)
    {
        ruby = other.GetComponent<RubyController>();
        if (ruby != null && ruby.Id == GameController.PlayAsID)
        {
            ruby.ChangeHealth(-1);
        }
    }

}
