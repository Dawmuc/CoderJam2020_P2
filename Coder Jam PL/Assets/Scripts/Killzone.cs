using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Killzone : MonoBehaviour
{
    [SerializeField] private ParticleSystem PikParticle = null;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Player")
        {
            if (!PlayerController.Instance.isDying)
            {
                PlayerController.Instance.isDying = true;
                PlayerController.Instance.PlayerDeath();
            }

            ParticleSystem pikParticle =  Instantiate(PikParticle, transform.position, Quaternion.identity) as ParticleSystem;
            Destroy(gameObject);
        }
    }
}
