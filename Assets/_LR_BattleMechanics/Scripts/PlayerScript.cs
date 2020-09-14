using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class PlayerScript : MonoBehaviour
{
    [SerializeField] EnemyScript enemy;
    [SerializeField] float moveSpeed, rotateSpeed = 10f, initialAtb = 20f, atbChargeRate = 2f, distThreshold = 4f;
    [SerializeField] Slider atb;
    [SerializeField] Ability[] abilities;

    private Vector3 move;
    private Transform cam;
    private Rigidbody rb;
    //private Animator anim;
    private float currentAtb;

    public Animator anim { get; private set; }
    public float DistThreshold { get => distThreshold; }
    public bool performMoves { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main.transform;
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
        
        //initialize the atb
        currentAtb = initialAtb;
        atb.maxValue = initialAtb;
    }

    // Update is called once per frame
    void Update()
    {
        GetInput();

        ChargeATB();
    }

    private void FixedUpdate()
    {
        Move();

        Facing();
    }

    void GetInput()
    {
        Vector3 worldMove = Vector3.zero;
        
        //get input
        worldMove.x = Input.GetAxis("Horizontal");
        worldMove.z = Input.GetAxis("Vertical");
        worldMove = worldMove.normalized * moveSpeed;
        worldMove.y = rb.velocity.y; //set the y value to the y velocity of the rigidbody

        //create a vector that is relative to camera, to make the movement intuitive
        move = Quaternion.Euler(0f, cam.eulerAngles.y, 0f) * worldMove;
        
        //loop through all the abilities array, and execute the Perform() method on each member.
        foreach (var ability in abilities)
        {
            ability.Perform(this, enemy, currentAtb);
        }
    }

    //this is for charging the ATB and update the ATB slider bar
    void ChargeATB()
    {
        if (currentAtb < initialAtb)
        {
            currentAtb += atbChargeRate * Time.deltaTime;
        }

        atb.value = currentAtb;
    }

    //method for consuming ATB, gets called by the Ability when performed.
    public void ConsumeATB(float value)
    {
        currentAtb -= value;
        atb.value = currentAtb;
    }

    void Move()
    {
        if (performMoves)
            return;

        rb.velocity = move;

        anim.SetFloat("Speed", rb.velocity.sqrMagnitude);
    }

    void Facing()
    {
        Vector3 dir = transform.forward;

        if (rb.velocity.sqrMagnitude > 0f)
        {
            dir = rb.velocity;
        }
        else
        {
            //if the enemy isn't null, then set the player facing the enemy
            if (enemy != null)
            {
                dir = enemy.transform.position - transform.position;
            }
        }
        
        //set the y direction value to 0, so we get the perfect XZ direction.
        dir.y = 0f;

        if (Quaternion.Angle(rb.rotation,Quaternion.LookRotation(dir)) > 10f)
            rb.rotation = Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(dir), rotateSpeed * Time.fixedDeltaTime);
    }
}

[System.Serializable]
public class Ability
{
    [SerializeField] int atbCost, damage;
    [SerializeField] KeyCode key;
    [SerializeField] bool holdable;
    [SerializeField] string trigger;

    //public int AtbCost { get => atbCost; }
    public KeyCode Key { get => key; }
    
    //method for performing the ability, gets called by the player update, but it will only be performed if the ATB is enough and the key is pressed.
    public void Perform(PlayerScript player, EnemyScript target, float atb)
    {
        if (atb < atbCost)
            return;

        if (holdable)
        {
            player.anim.SetBool(trigger, (Input.GetKey(key) && atb > atbCost));

            player.performMoves = (Input.GetKey(key) && atb > atbCost);

            if (Input.GetKey(key))
            {
                player.ConsumeATB(atbCost * Time.deltaTime);
            }
            return;
        }

        if (Input.GetKeyDown(key))
        {
            if ((target.transform.position - player.transform.position).sqrMagnitude > Mathf.Pow(player.DistThreshold,2f))
            {
                //out of range, then do tween and play jump animation to approach the enemy
                player.anim.Play("Jump");
                player.performMoves = true;
                Vector3 targetPos = target.transform.position + target.transform.forward * player.DistThreshold;

                //do tween and perform
                player.transform.DOJump(targetPos, 0.5f, 1, 0.25f).OnComplete(delegate
                {
                    player.anim.SetTrigger(trigger);
                    player.performMoves = false;
                    target.Hit(damage, player.transform.position);
                });
            }
            else
            {
                //in range, perform ability right away
                player.anim.SetTrigger(trigger);
                player.performMoves = true;
                target.Hit(damage, player.transform.position);
            }
            
            //consume the ATB
            player.ConsumeATB(atbCost);
        }
    }
}
