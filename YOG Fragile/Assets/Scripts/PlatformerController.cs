using UnityEngine;
using System.Collections;

public class PlatformerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Maximum horizontal movement speed")]
    public float moveSpeed = 8f;

    [Tooltip("Acceleration rate when speeding up")]
    public float acceleration = 50f;

    [Tooltip("Deceleration rate when slowing down")]
    public float deceleration = 50f;

    [Tooltip("Air acceleration (typically lower than ground)")]
    public float airAcceleration = 30f;

    [Tooltip("Air deceleration (typically lower than ground)")]
    public float airDeceleration = 30f;

    [Header("Jump Settings")]
    [Tooltip("Maximum jump height in Unity units")]
    public float jumpHeight = 4f;

    [Tooltip("Speed boost when jumping")]
    public float jumpVelocityXBoost = 20f;

    [Tooltip("Time to reach jump apex (affects gravity)")]
    public float jumpTimeToApex = 0.4f;

    [Tooltip("Grace period for jumping after leaving ground")]
    public float coyoteTime = 0.1f;

    [Tooltip("Input buffer time for jumps")]
    public float jumpBufferTime = 0.1f;

    [Header("Physics Settings")]
    [Tooltip("Gravity multiplier when moving upward")]
    public float gravityScale = 1f;

    [Tooltip("Gravity multiplier when falling (makes falling feel snappier)")]
    public float fallGravityScale = 1.5f;

    [Tooltip("Maximum falling speed")]
    public float maxFallSpeed = 20f;

    [Header("Ground Detection")]
    [Tooltip("Layers considered as ground")]
    public LayerMask groundLayerMask = 1;

    [Tooltip("Distance to check for ground below character")]
    public float groundCheckDistance = 0.1f;

    [Tooltip("Number of raycasts for ground detection")]
    [Range(1, 5)]
    public int groundCheckRayCount = 3;

    [Tooltip("Maximum angle considered as ground (in degrees)")]
    [Range(0f, 90f)]
    public float maxGroundAngle = 45f;

    [Header("Wall Settings")]
    [Tooltip("Speed when walking on the wall")]
    public float wallMoveSpeed = 4f;

    [Tooltip("Acceleration rate while climbing/sliding on walls")]
    public float wallAcceleration = 40f;

    [Tooltip("Deceleration rate when stopping on a wall")]
    public float wallDeceleration = 60f;

    [Tooltip("Layers considered as walls")]
    public LayerMask wallLayerMask;

    [Tooltip("Distance to check for walls")]
    public float wallCheckDistance = 0.1f;

    [Tooltip("How much gravity is applied while sticking (0 = complete stick)")]
    public float wallSlideGravityMultiplier = 0f;

    [Tooltip("Force of the wall jump (X = away from wall, Y = upward)")]
    public Vector2 wallJumpForce = new Vector2(15f, 15f);

    [Tooltip("How far from a wall the player can still trigger a wall jump")]
    public float wallJumpDetectionDistance = 0.5f;

    public bool wallOnRight { get; private set; }
    public bool wallOnLeft { get; private set; }

    [Header("Ceiling Settings")]
    public float ceilingMoveSpeed = 8f;

    public float ceilingAcceleration = 30f;

    public float ceilingDeceleration = 100f;

    [Tooltip("Distance to check for ceiling above character")]
    public float ceilingCheckDistance = 0.1f;

    [Tooltip("Layers considered as ceiling")]
    public LayerMask ceilingLayerMask;

    [Header("Mantle Settings")]
    [Tooltip("Horizontal offset for the ledge check (should be slightly further than wall check)")]
    public float ledgeCheckForwardDistance = 0.2f;
    [Tooltip("Vertical offset to check if there is empty space above the ledge")]
    public float ledgeCheckHeight = 0.1f;
    [Tooltip("How far the player 'teleports' onto the platform")]
    public Vector2 mantleOffset = new Vector2(0.8f, 1.2f);
    [Tooltip("How long the mantle takes")]
    public float mantleTime = 0.5f;

    // Component references
    [HideInInspector] public Rigidbody2D rb2d;
    [HideInInspector] public BoxCollider2D boxCollider;
    [HideInInspector] public StaminaSystem stamina;


    // Movement state properties
    [HideInInspector] public Vector2 velocity;
    [HideInInspector] public bool isGrounded;
    [HideInInspector] public bool wasGrounded;
    [HideInInspector] public bool isWallSticking;
    [HideInInspector] public bool isCeilingSticking;
    [HideInInspector] public bool isMantling;

    // Timing counters
    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    private float wallJumpLockoutTimer;
    private const float WallJumpLockoutDuration = 0.2f;

    // Input tracking
    [HideInInspector] public Vector2 moveInput;
    [HideInInspector] public bool jumpInput;
    [HideInInspector] public bool jumpInputPressed;
    [HideInInspector] public bool stickInput;

    // Physics calculations
    private float gravity;
    private float jumpVelocity;

    // Events for external systems
    public System.Action OnJump;
    public System.Action OnLand;
    public System.Action OnTakeDamage;


    protected virtual void Awake()
    {
        InitializeComponents();
        CalculatePhysics();
        
    }

    protected virtual void InitializeComponents()
    {
        rb2d = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();        
        stamina = GetComponent<StaminaSystem>();
        Cursor.visible = false;
    }

    protected virtual void CalculatePhysics()
    {
        gravity = -(2 * jumpHeight) / Mathf.Pow(jumpTimeToApex, 2);
        jumpVelocity = Mathf.Abs(gravity) * jumpTimeToApex;

        // Debug.Log($"Calculated Physics - Gravity: {gravity}, Jump Velocity: {jumpVelocity}");
    }

    protected virtual void Update()
    {
        UpdateGroundedState();

        UpdateTimers();

        if (stamina != null && stamina.IsDepleted) 
        {
            moveInput = Vector2.zero;
            return;
        }

        if (velocity.x > 0f)
        {
            transform.eulerAngles = new Vector3(0f, 0f, 0f);
        }
        else if (velocity.x < 0f)
        {
            transform.eulerAngles = new Vector3(0f, 180f, 0f);
        }

        if (isWallSticking)
        {
            if (velocity.y > 0f)
            {
                transform.eulerAngles = new Vector3(0f, 0f, 0f);
            }
            else if (velocity.y < 0f)
            {
                transform.eulerAngles = new Vector3(0f, 180f, 180f);
            }
            if (velocity.y > 0f && !IsTouchingWallRight())
            {
                transform.eulerAngles = new Vector3(0f, 180f, 0f);
            }
            else if (velocity.y < 0f && !IsTouchingWallRight())
            {
                transform.eulerAngles = new Vector3(0f, 0f, 180f);
            }
        }
    }


    protected virtual void FixedUpdate()
    {           
        isCeilingSticking = !isGrounded && IsTouchingCeiling() && stickInput;
     
        isWallSticking = !isGrounded && IsTouchingWall() && wallJumpLockoutTimer <= 0 && stickInput;

        if (isWallSticking)
        {
            CheckForLedge();
        }

        if (!isWallSticking)
        {
            wallOnRight = false;
            wallOnLeft = false;
        }

        ApplyMovement();
        ApplyGravity();
        ApplyJump();

        rb2d.linearVelocity = velocity;
    }
    protected virtual void UpdateGroundedState()
    {
        wasGrounded = isGrounded;
        isGrounded = IsGrounded();

        if (isGrounded && !wasGrounded)
        {
            OnLand?.Invoke();
        }
    }
    protected virtual void UpdateTimers()
    {
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        jumpBufferCounter -= Time.deltaTime;

        wallJumpLockoutTimer -= Time.deltaTime;
    }

    protected virtual void ApplyMovement()
    {
        float targetVelocityX = moveInput.x * (isCeilingSticking ? ceilingMoveSpeed : moveSpeed);
        float currentVelocityX = velocity.x;

        float accel;

        if (isCeilingSticking)
        {
            accel = ceilingAcceleration;
        }
        else
        {
            accel = isGrounded ? acceleration : airAcceleration;
        }

        float decel;

        if (isCeilingSticking)
        {
            decel = ceilingDeceleration;
        }
        else
        {
            decel = isGrounded ? deceleration : airDeceleration;
        }

        bool accelerating = (Mathf.Sign(targetVelocityX) == Mathf.Sign(currentVelocityX)) ||
                           Mathf.Approximately(currentVelocityX, 0);

        float rate = accelerating ? accel : decel;

        if (isWallSticking)
        {
            velocity.x = 0;

            float targetVelocityY = moveInput.y * wallMoveSpeed;
            float currentVelocityY = velocity.y;

            bool isVerticalAccelerating = (Mathf.Sign(targetVelocityY) == Mathf.Sign(currentVelocityY)) ||
                                            Mathf.Approximately(currentVelocityY, 0);

            float vRate = isVerticalAccelerating ? wallAcceleration : wallDeceleration;

            velocity.y = Mathf.MoveTowards(currentVelocityY, targetVelocityY, vRate * Time.fixedDeltaTime);
        }
        else
        {
            if (!Mathf.Approximately(targetVelocityX, 0))
            {
                velocity.x = Mathf.MoveTowards(currentVelocityX, targetVelocityX, rate * Time.fixedDeltaTime);
            }
            else
            {
                velocity.x = Mathf.MoveTowards(currentVelocityX, 0, rate * Time.fixedDeltaTime);
            }

            if (isCeilingSticking)
            {
                velocity.y = 0;
            }
        }
    }

    protected virtual void ApplyGravity()
    {
        float gravityMultiplier = velocity.y > 0 ? gravityScale : fallGravityScale;

        if (isCeilingSticking)
        {
            gravityMultiplier *= -1;
        }

        if (isWallSticking || isMantling)
        {
            if (Mathf.Abs(moveInput.y) > 0.1f)
            {
                return;
            }

            gravityMultiplier = wallSlideGravityMultiplier;
            velocity.y = 0;
        }

        velocity.y += gravity * gravityMultiplier * Time.fixedDeltaTime;

        if (velocity.y < -maxFallSpeed)
        {
            velocity.y = -maxFallSpeed;
        }
    }

    protected virtual void ApplyJump()
    {
        bool wantsToJump = jumpBufferCounter > 0;

        if (wantsToJump)
        {
            float wallDir = IsTouchingWallRight() ? -1 : 1;
            if (isWallSticking || IsNearWall(out wallDir))
            {
                float jumpYDir = 0.7f;
                if (moveInput.y > 0)
                {
                    jumpYDir = 1;                    
                }
                else if (moveInput.y < 0)
                {

                    jumpYDir = -1;
                }
                else if (moveInput.y == 0)
                {
                    jumpYDir = 0.7f;
                }
                    wallJumpLockoutTimer = WallJumpLockoutDuration;


                velocity = new Vector2(wallJumpForce.x * wallDir, wallJumpForce.y * jumpYDir);


                jumpBufferCounter = 0;
                OnJump?.Invoke();
                return;
            }
            else if (isCeilingSticking)
            {
                velocity.y = -jumpVelocity;
                isCeilingSticking = false;
                jumpBufferCounter = 0;
                OnJump?.Invoke();
                return;
            }
            else if (coyoteTimeCounter > 0)
            {
                velocity.y = jumpVelocity;

                if (moveInput.x > 0)
                {
                    velocity.x += jumpVelocityXBoost;
                }
                else if (moveInput.x < 0)
                {
                    velocity.x -= jumpVelocityXBoost;
                }

                coyoteTimeCounter = 0;
                jumpBufferCounter = 0;
                OnJump?.Invoke();
            }
        }
    }

    protected virtual bool IsGrounded()
    {
        Bounds bounds = boxCollider.bounds;

        for (int i = 0; i < groundCheckRayCount; i++)
        {
            float lerpFactor = groundCheckRayCount == 1 ? 0.5f : (float)i / (groundCheckRayCount - 1);
            Vector2 rayOrigin = new Vector2(
                Mathf.Lerp(bounds.min.x + 0.1f, bounds.max.x - 0.1f, lerpFactor),
                bounds.min.y
            );

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, groundCheckDistance, groundLayerMask);


            if (hit.collider != null)
            {
                float surfaceAngle = Vector2.Angle(hit.normal, Vector2.up);

                if (surfaceAngle <= maxGroundAngle)
                {
                    return true;
                }
            }
        }

        return false;
    }

    protected virtual bool IsTouchingWall()
    {
        Bounds bounds = boxCollider.bounds;

        wallOnRight = Physics2D.Raycast(new Vector2(bounds.max.x, bounds.center.y), Vector2.right, wallCheckDistance, wallLayerMask);
        wallOnLeft = Physics2D.Raycast(new Vector2(bounds.min.x, bounds.center.y), Vector2.left, wallCheckDistance, wallLayerMask);

        return wallOnRight || wallOnLeft;
    }

    protected virtual bool IsTouchingWallRight()
    {
        return wallOnRight;
    }

    protected virtual bool IsTouchingCeiling()
    {
        Bounds bounds = boxCollider.bounds;
        float raySpacing = (bounds.max.x - bounds.min.x) / (groundCheckRayCount - 1);

        for (int i = 0; i < groundCheckRayCount; i++)
        {
            Vector2 rayOrigin = new Vector2(bounds.min.x + (raySpacing * i), bounds.max.y);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, ceilingCheckDistance, ceilingLayerMask);

            if (hit.collider != null) return true;
        }
        return false;
    }

    protected virtual bool CheckForLedge()
    {
        if (!isWallSticking || moveInput.y <= 0) return false;

        Bounds bounds = boxCollider.bounds;
        float direction = wallOnRight ? 1f : -1f;

        Vector2 rayOrigin = new Vector2(direction > 0 ? bounds.max.x - 0.05f : bounds.min.x + 0.05f,bounds.max.y + ledgeCheckHeight);
        RaycastHit2D headHit = Physics2D.Raycast(rayOrigin, Vector2.right * direction, ledgeCheckForwardDistance, wallLayerMask);

        Debug.DrawRay(rayOrigin, Vector2.right * direction * ledgeCheckForwardDistance, Color.red);

        if (headHit.collider == null)
        {
            PerformMantle(direction);
            return true;
        }

        return false;
    }

    protected virtual void PerformMantle(float direction)
    {
        if (isMantling)
        {
            return;
        }

        StartCoroutine(MantleRoutine(direction));
    }

    private IEnumerator MantleRoutine(float direction)
    {
        isMantling = true;

        Vector2 startingPos = rb2d.position;
        velocity = Vector2.zero;

        Vector2 finalPos = startingPos;
        finalPos.y += mantleOffset.y;

        yield return new WaitForSeconds(mantleTime);

        finalPos.x += mantleOffset.x * direction;

        rb2d.position = finalPos;

        isWallSticking = false;
        isMantling = false;
    }
    protected virtual bool IsNearWall(out float wallDirection)
    {
        if (isGrounded)
        {
            wallDirection = 0f;
            return false;
        }

        Bounds bounds = boxCollider.bounds;

        bool hitRight = Physics2D.Raycast(new Vector2(bounds.max.x, bounds.center.y), Vector2.right, wallJumpDetectionDistance, wallLayerMask);
        Debug.DrawRay(new Vector2(bounds.max.x, bounds.center.y), Vector2.right * wallJumpDetectionDistance, hitRight ? Color.green : Color.red);

        if (hitRight)
        {
            wallDirection = -1f;
            return true;
        }

        bool hitLeft = Physics2D.Raycast(new Vector2(bounds.min.x, bounds.center.y), Vector2.left, wallJumpDetectionDistance, wallLayerMask);
        Debug.DrawRay(new Vector2(bounds.min.x, bounds.center.y), Vector2.left * wallJumpDetectionDistance, hitLeft ? Color.green : Color.red);

        if (hitLeft)
        {
            wallDirection = 1f;
            return true;
        }

        wallDirection = 0f;
        return false;
    }

    public virtual void SetMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    public virtual void SetJumpInput(bool pressed, bool held)
    {
        bool previousJump = jumpInput;
        jumpInput = held;
        jumpInputPressed = pressed && !previousJump;

        if (jumpInputPressed)
        {
            jumpBufferCounter = jumpBufferTime;
        }
    }
    public virtual void SetStickInput(bool isHeld)
    {
        stickInput = isHeld;
    }
}
