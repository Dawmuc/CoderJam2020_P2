﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    private Rigidbody2D rb2d;
    private Vector2 velocity;
    private SpriteRenderer spriteRend;

    private bool canMove = true;

    [Header("Gravity")]
    [SerializeField] private float Gravity = 20f;
    [SerializeField] private float FallMaxSpeed = 50f;

    [Header("Movements")]
    //[Tooltip("déplacement du joueur")]
    [SerializeField] private float AccelerationSpeed = 5f;
    [SerializeField] private float MaxSpeed = 10f;
    [SerializeField] private float DecelerationSpeed = 10f;
    private float currentSpeed = 0f;
    private float orientX = 1f;

    [Header("Jump")]
    [SerializeField] private AnimationCurve AnimCurveJump = null;
    [SerializeField] private float DurationJump = 0.5f;
    [SerializeField] private float JumpHigh = 15f;
    private Coroutine JumpCorout;
    private bool isJumping = false;

    [Header("GroundCheck")]
    [SerializeField] private LayerMask groundMask = default;
    [SerializeField] private Transform GroundCheck = null;
    private bool isGround = false;

    [Header("PlayerDeath")]
    [HideInInspector] public bool isDying = false;
    [SerializeField] private ParticleSystem PlayerDeathParticle = null;
    public float durationBeforeDying = 0.5f;
    [SerializeField] private float durationAnimColorDeath = 0.1f;
    [SerializeField] private AnimationCurve AnimCurveDeath = null;
    [SerializeField] private Color AnimColorDeath = Color.red;

    [Header("Respawn")]
    private List<Vector3> liRespawnPos = new List<Vector3>();
    [SerializeField] private float DurationBeforeRespawn = 1f;
    private int idRespawn = 0;
    [SerializeField] private float DurationAnimRespawn = 0.5f;
    [SerializeField] private AnimationCurve AnimCurveRespawn = null;



    private float moveHorizontal;
    private bool AButtonDown;


    #region Unity Methode
    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        spriteRend = GetComponent<SpriteRenderer>();
        liRespawnPos.Add(transform.position);
    }

    void Update()
    {
        isGround = Physics2D.Linecast(transform.position, GroundCheck.position, groundMask);

        if (!canMove)
        {
            rb2d.velocity = Vector2.zero;
            currentSpeed = 0f;
            return;
        }

        UpdateControl();
        UpdateGravity();
        UpdateMove();
        UpdateJump();
        rb2d.velocity = velocity;
    }
    #endregion

    #region Control
    private void UpdateControl()
    {
        moveHorizontal = Input.GetAxis("Horizontal");
        AButtonDown = Input.GetButtonDown("Fire1");
    }
    #endregion

    #region Gravity
    private void UpdateGravity()
    {
        if (isJumping)
            return;

        if (isGround)
        {
            velocity.y = 0f;
            return;
        }
        velocity.y -= Gravity * Time.deltaTime;
        if (velocity.y < -FallMaxSpeed)
        {
            velocity.y = -FallMaxSpeed;
        }
    }
    #endregion

    #region Movement
    private void UpdateMove()
    {
        if(Mathf.Abs(moveHorizontal) > 0.1f)
        {

            if (currentSpeed > 0 && orientX * moveHorizontal < 0)
                currentSpeed -= DecelerationSpeed * Time.deltaTime;
            else
            {
                orientX = Mathf.Sign(moveHorizontal);

                currentSpeed += AccelerationSpeed * Time.deltaTime;
                if (currentSpeed > MaxSpeed)
                    currentSpeed = MaxSpeed;
            }
        }
        else
        {
            currentSpeed -= DecelerationSpeed * Time.deltaTime;
            if (currentSpeed <= 0f)
                currentSpeed = 0f;
        }
        velocity.x = currentSpeed * orientX;
    }
    #endregion

    #region Jump
    private void UpdateJump()
    {
        if(AButtonDown && isGround)
        {
            JumpCorout = StartCoroutine(JumpCoroutine());
        }
    }

    private void stopJumpCorout()
    {
        if (JumpCorout != null)
        {
            velocity.y = 0f;
            isJumping = false;
            StopCoroutine(JumpCorout);
        }
    }

    private IEnumerator JumpCoroutine()
    {
        isJumping = true;

        float startVelocityY = velocity.y;
        float startTime = Time.time;

        while (Time.time < startTime + DurationJump)
        {
            velocity.y = Mathf.Lerp(startVelocityY, startVelocityY + JumpHigh, AnimCurveJump.Evaluate((Time.time - startTime) / DurationJump));
            yield return null;
        }
        isJumping = false;
    }
    #endregion

    #region playerDeath
    public void PlayerDeath()
    {
        StartCoroutine(AnimDeath());
    }

    private IEnumerator AnimDeath()
    {
        float startTimeDeath = Time.time;
        while (isDying)
        {
            Color startColor = spriteRend.color;
            float startTime = Time.time;
            while(Time.time < startTime + durationAnimColorDeath)
            {
                spriteRend.color = Color.Lerp(startColor, AnimColorDeath, AnimCurveDeath.Evaluate((Time.time - startTime) / durationAnimColorDeath));

                if (Time.time > startTimeDeath + durationBeforeDying)
                {
                    stopJumpCorout();
                    StartCoroutine(RespawnPlayer());
                    isDying = false;
                }

                yield return null;
            }
            spriteRend.color = startColor;

            yield return null;
        }
    }

    private IEnumerator RespawnPlayer()
    {
        ParticleSystem playerDeathParticle = Instantiate(PlayerDeathParticle, transform.position, Quaternion.identity) as ParticleSystem;
        canMove = false;
        spriteRend.enabled = false;
        GetComponent<CircleCollider2D>().enabled = false;
        transform.localScale = Vector3.zero;

        yield return new WaitForSeconds(DurationBeforeRespawn);

        transform.position = liRespawnPos[idRespawn];
        spriteRend.enabled = true;
        GetComponent<CircleCollider2D>().enabled = true;

        float currentScale = 0f;
        float startTime = Time.time;
        while(Time.time < startTime + DurationAnimRespawn)
        {
            currentScale = 1f * AnimCurveRespawn.Evaluate((Time.time - startTime) / DurationAnimRespawn);
            Debug.Log(currentScale);

            transform.localScale = new Vector3(currentScale, currentScale, currentScale);
            yield return null;
        }
        transform.localScale = Vector3.one;

        yield return new WaitForSeconds(0.3f);


        canMove = true;
    }
    #endregion

}
