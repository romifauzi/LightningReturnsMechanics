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

        worldMove.x = Input.GetAxis("Horizontal");
        worldMove.z = Input.GetAxis("Vertical");
        worldMove = worldMove.normalized * moveSpeed;
        worldMove.y = rb.velocity.y;

        move = Quaternion.Euler(0f, cam.eulerAngles.y, 0f) * worldMove;

        foreach (var ability in abilities)
        {
            ability.Perform(this, enemy, currentAtb);
        }
    }

    void ChargeATB()
    {
        if (currentAtb < initialAtb)
        {
            currentAtb += atbChargeRate * Time.deltaTime;
        }

        atb.value = currentAtb;
    }

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
            if (enemy != null)
            {
                dir = enemy.transform.position - transform.position;
            }
        }

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
                player.anim.SetTrigger(trigger);
                player.performMoves = true;
                target.Hit(damage, player.transform.position);
            }

            player.ConsumeATB(atbCost);
        }
    }
}
