using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    [SerializeField] int health = 10;
    [SerializeField] ParticleSystem hitFx;

    private Animator anim;
    private Collider col;
    private Vector3 lastHitPos;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        col = GetComponent<Collider>();
    }
    
    //this method is for applying damage to the enemy, and it is called by the Ability class of the Player, hitPos would be the player position
    public void Hit(int damage, Vector3 hitPos)
    {
        if (health <= 0)
            return;
        
        //this will get the closest point on the enemy collider from the player position, and its going to be used for re-position the particle hit fx
        lastHitPos = col.ClosestPoint(hitPos + Vector3.up);

        health -= damage;

        if (health <= 0)
        {
            Dead();
            return;
        }

        Invoke("HitAnimation", 0.15f);
        Invoke("PlayHitFx", 0.25f);
    }

    void Dead()
    {
        anim.SetTrigger("Dead");
        enabled = false;
    }

    void HitAnimation()
    {
        anim.SetTrigger("Hit");
    }

    void PlayHitFx()
    {
        if (hitFx != null)
        {
            hitFx.transform.position = lastHitPos;
            hitFx.Play();
        }
    }
}
