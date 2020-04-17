using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
	public static PlayerController Instance { get; private set; }

	private Rigidbody2D rb2d;
	private Vector2 velocity;
	private SpriteRenderer spriteRend;

	private bool canMove = true;

	private CameraManager cameraManager;

	[Header("Gravity")]
    [SerializeField] private float Gravity = 20f;
    [SerializeField] private float FallMaxSpeed = 50f;

    [Header("Movements")]
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

	[Header("Throw")]
	[SerializeField] private float throwSpeed = 20.0f;
	[SerializeField] private float throwTime = 1.0f;
	[SerializeField] private Vector3 smallThrowerScale = new Vector3(1.5f, 1.5f, 1.5f);
	[SerializeField] private Vector3 normalThrowerScale = new Vector3(3f, 3f, 3f);
	private bool isBeingThrown;
	private bool isChoosingThrowingDir;
	private bool playerInPosition;
	private Transform ThrowerTransform; 

	[Header("GroundCheck")]
    [SerializeField] private LayerMask groundMask = default;
    [SerializeField] private Transform GroundCheck = null;
    private bool isGround = false;

    [Header("PlayerDeath")]
    [SerializeField] private ParticleSystem PlayerDeathParticle = null;
    [SerializeField] private float durationAnimColorDeath = 0.1f;
    [SerializeField] private AnimationCurve AnimCurveDeath = null;
    [SerializeField] private Color AnimColorDeath = Color.red;
    [HideInInspector] public bool isDying = false;
    public float durationBeforeDying = 0.5f;
	private int deathCount = 0;

    [Header("Respawn")]
    [SerializeField] private float DurationBeforeRespawn = 1f;
    [SerializeField] private float DurationAnimRespawn = 0.5f;
    [SerializeField] private AnimationCurve AnimCurveRespawn = null;
    private List<Vector3> liRespawnPos = new List<Vector3>();

	[Header("Canva")]
	[SerializeField] private GameObject canva;
	[SerializeField] private Text text;

    private float moveHorizontal;
	private float moveVertical;
	private bool AButtonDown;

	// player bounce effect
	private Vector3 playerNormalScale;
	private Vector3 playerSmallScale;
	private bool p2;

	private bool endGame;

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
		cameraManager = FindObjectOfType<CameraManager>();
		playerNormalScale = transform.localScale;
		playerSmallScale = playerNormalScale - new Vector3(0.2f, 0.2f, 0.2f);
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
		UpdateThrowingInstruction();
		UpdateThrowerGraph();
		rb2d.velocity = velocity;
	}
    #endregion

    #region Control
    private void UpdateControl()
    {
        moveHorizontal = Input.GetAxis("Horizontal");
		moveVertical = Input.GetAxis("Vertical");
		AButtonDown = Input.GetButtonDown("Fire1");
	}
    #endregion

    #region Gravity
    private void UpdateGravity()
    {
        if (isJumping || isBeingThrown || isChoosingThrowingDir || endGame)
            return;

        if (isGround)
        {
            velocity.y = 0f;
            return;
        }

        velocity.y -= Gravity * Time.deltaTime;

        if (velocity.y < -FallMaxSpeed)
			velocity.y = -FallMaxSpeed;
    }
    #endregion

    #region Movement
    private void UpdateMove()
    {
		if (isBeingThrown || isChoosingThrowingDir)
			return;
		else if (endGame)
			velocity.x = MaxSpeed;
		else if (Mathf.Abs(moveHorizontal) > 0.1f)
		{

			if (currentSpeed > 0 && orientX * moveHorizontal < 0)
				currentSpeed -= DecelerationSpeed * Time.deltaTime;
			else
			{
				orientX = Mathf.Sign(moveHorizontal);

				currentSpeed += AccelerationSpeed * Time.deltaTime;
				if (currentSpeed > MaxSpeed)
					currentSpeed = Mathf.Lerp(currentSpeed, MaxSpeed, 0.1f);
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
        if(AButtonDown && isGround && !endGame)
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

        while (Time.time < startTime + DurationJump && isJumping)
        {
            velocity.y = Mathf.Lerp(startVelocityY, startVelocityY + JumpHigh, AnimCurveJump.Evaluate((Time.time - startTime) / DurationJump));
            yield return null;
        }
        isJumping = false;
    }
	#endregion

	#region Throwing
	private void UpdateThrowingInstruction()
	{
		if (isChoosingThrowingDir)
		{
			velocity = Vector2.zero;
			Vector2 dir = new Vector2(moveHorizontal, moveVertical).normalized;
			StartCoroutine(GoToThrowerCenter());

			if (AButtonDown && playerInPosition)
				StartCoroutine(ThowPlayer(dir));
		}
	}

	private IEnumerator GoToThrowerCenter()
	{
		playerInPosition = false;

		while (((Vector2)ThrowerTransform.position - (Vector2)transform.position).magnitude > 0.01)
		{
			transform.position = Vector2.Lerp(transform.position, ThrowerTransform.position, 0.01f);
			yield return null;
		}

		playerInPosition = true;
	}

	private IEnumerator ThowPlayer(Vector2 dir)
	{
		isBeingThrown = true;

		isChoosingThrowingDir = false;
		float t = 0.0f;

		while (t < throwTime && isBeingThrown)
		{
			velocity = dir * throwSpeed * Time.deltaTime;
			currentSpeed = Mathf.Abs(velocity.x);
			orientX = Mathf.Sign(velocity.x);
			t += Time.deltaTime;
			yield return null;
		}

		isBeingThrown = false;
	}
	#endregion

	#region Thrower Graph
	private void UpdateThrowerGraph()
	{
		if (ThrowerTransform == null)
			return;

		if (isChoosingThrowingDir)
			ThrowerTransform.localScale = Vector3.Lerp(ThrowerTransform.localScale, smallThrowerScale, 0.05f);
		else if ((normalThrowerScale - ThrowerTransform.localScale).magnitude > 0.001)
			ThrowerTransform.localScale = Vector3.Lerp(ThrowerTransform.localScale, normalThrowerScale, 0.3f);
		else
			ThrowerTransform = null;
	}
	#endregion

	#region player Death
	public void PlayerDeath()
    {
		StartCoroutine(AnimDeath());
		deathCount++;
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
		isBeingThrown = false;
		isChoosingThrowingDir = false;
	}

    private IEnumerator RespawnPlayer(int index = 100000)
    {
        ParticleSystem playerDeathParticle = Instantiate(PlayerDeathParticle, transform.position, Quaternion.identity) as ParticleSystem;
        canMove = false;
        spriteRend.enabled = false;
        GetComponent<CircleCollider2D>().enabled = false;
        transform.localScale = Vector3.zero;

        yield return new WaitForSeconds(DurationBeforeRespawn);

		transform.position = index < liRespawnPos.Count ? liRespawnPos[index] : liRespawnPos.Last();
        spriteRend.enabled = true;
        GetComponent<CircleCollider2D>().enabled = true;

        float currentScale = 0f;
        float startTime = Time.time;
        while(Time.time < startTime + DurationAnimRespawn)
        {
            currentScale = 1f * AnimCurveRespawn.Evaluate((Time.time - startTime) / DurationAnimRespawn);

            transform.localScale = new Vector3(currentScale, currentScale, currentScale);
            yield return null;
        }
        transform.localScale = Vector3.one;

        yield return new WaitForSeconds(0.3f);

        canMove = true;
    }
	#endregion

	#region Player Bounce
	private IEnumerator PlayerBounce()
	{
		while ((transform.localScale - playerSmallScale).magnitude > 0.001 && !p2)
		{
			transform.localScale = Vector3.Lerp(transform.localScale, playerSmallScale, 0.6f);
			yield return null;
		}
		p2 = true;
		while ((playerNormalScale - transform.localScale).magnitude > 0.001 && p2)
		{
			transform.localScale = Vector3.Lerp(transform.localScale, playerNormalScale, 0.6f);
			yield return null;
		}
		p2 = false;
	}
	#endregion

	#region Collision
	private void OnTriggerEnter2D(Collider2D col)
	{
		if (col.gameObject.tag == "Thrower" && ThrowerTransform != col.transform)
		{
			isChoosingThrowingDir = true;
			isJumping = false;
			ThrowerTransform = col.transform;
		}
		else if (col.gameObject.tag == "Checkpoint")
		{
			liRespawnPos.Add(col.transform.position);
		}
		else if (col.gameObject.tag == "End" && isDying == false)
		{
			EndGame();
		}
	}

	private void OnCollisionEnter2D(Collision2D col)
	{
		isBeingThrown = false;
		StopCoroutine(PlayerBounce());
		StartCoroutine(PlayerBounce());
	}
	#endregion

	#region Game End
	private void EndGame()
	{
		endGame = true;
		cameraManager.enabled = false;
		canva.SetActive(true);
		text.text = $"{deathCount} perished";
		StartCoroutine(reloadSceneAfterDelay());
	}

	private IEnumerator reloadSceneAfterDelay()
	{
		float t = 0.0f;

		while (t < 3.0f)
		{
			t += Time.deltaTime;
			yield return null;
		}

		SceneManager.LoadScene(0);
	}

	#endregion
}