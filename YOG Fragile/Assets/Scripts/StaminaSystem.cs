using System.Collections;
using UnityEngine;

/// <summary>
/// Tracks player stamina and drains it based on actions.
/// Attach to the same GameObject as PlatformerController.
///
/// Drain rates:
///   Jump         : 100 stamina flat
///   Wall walking : 33.33/s  (100 stamina per 3 seconds)
///   Wall sitting : 9.09/s   (100 stamina per 11 seconds)
/// </summary>
[RequireComponent(typeof(PlatformerController))]
public class StaminaSystem : MonoBehaviour
{
    [Header("Stamina Settings")]
    [Tooltip("Maximum and starting stamina")]
    public float maxStamina = 800f;

    [Tooltip("Stamina drained per jump")]
    public float jumpStaminaCost = 100f;

    [Tooltip("Stamina drained per second while actively moving on a wall (100 per 3 s)")]
    public float wallWalkDrainRate = 100f / 3f;   // ~= 33.33/s

    [Tooltip("Stamina drained per second while stationary on a wall (100 per 11 s)")]
    public float wallSitDrainRate = 100f / 11f;   // ~=  9.09/s

    [Tooltip("Stamina drained per second while actively moving on a ceiling (100 per 3 s)")]
    public float CeilingWalkDrainRate = 100f / 3f;   // ~= 33.33/s

    [Tooltip("Stamina drained per second while stationary on a ceiling (100 per 11 s)")]
    public float CeilingSitDrainRate = 100f / 11f;   // ~=  9.09/s

    [Header("Failsafe Settings")]
    [Tooltip("Max time allowed to stay in 'depleted' state before forcing a reset (even if not landed)")]
    public float maxDepletedTime = 3.0f;

    private float depletionTimer;

    public float CurrentStamina { get; private set; }
    public bool IsDepleted { get; private set; }

    private bool waitingForLanding = false;

    public float respawnPosX;
    public float respawnPosY;


    /// Fired every frame the stamina value changes. Arg = 0..1 normalised.
    public System.Action<float> OnStaminaChanged;

    /// Fired once when stamina hits zero
    public System.Action OnStaminaDepleted;

    public System.Action OnShatterSound;

    public static System.Action OnGlobalReset;

    private Coroutine activeDepletionRoutine;
    public Coroutine activeShatterRoutine;

    private PlatformerController controller;

    private Animator playerAnimator;

    private void Awake()
    {
        controller = GetComponent<PlatformerController>();
        playerAnimator = GetComponent<Animator>();

        CurrentStamina = maxStamina;
    }

    private void Start()
    {
        controller.OnJump += HandleJump;
    }

    private void OnDisable()
    {
        controller.OnJump -= HandleJump;
    }

    private void Update()
    {
        if (IsDepleted)
        {
            controller.stickInput = false;

            depletionTimer -= Time.deltaTime;

            if (waitingForLanding && depletionTimer <= 0)
            {
                Debug.LogWarning("[StaminaSystem] Failsafe: Shatter triggered by timer!");
                ForceShatter();
            }

            return;
        }

        DrainWallStamina();
        DrainCeilingStamina();
    }

    /// <summary>Called every frame the player is wall-sticking.</summary>
    private void DrainWallStamina()
    {
        if (!controller.isWallSticking) return;

        // "Walking" = vertical input present; "sitting" = no vertical input.
        bool isMovingOnWall = Mathf.Abs(controller.moveInput.y) > 0.1f;

        float rate = isMovingOnWall ? wallWalkDrainRate : wallSitDrainRate;

        Drain(rate * Time.deltaTime);
    }

    private void DrainCeilingStamina()
    {
        if (!controller.isCeilingSticking) return;
        bool isMovingOnCeiling = Mathf.Abs(controller.moveInput.x) > 0.1f;

        float rate = isMovingOnCeiling ? CeilingWalkDrainRate : CeilingSitDrainRate;

        Drain(rate * Time.deltaTime);
    }

    private void HandleJump()
    {
        Drain(jumpStaminaCost);
    }

    private void Drain(float amount)
    {
        float previous = CurrentStamina;
        CurrentStamina = Mathf.Max(0f, CurrentStamina - amount);

        // Only fire events if the value actually changed.
        if (Mathf.Approximately(previous, CurrentStamina)) return;

        OnStaminaChanged?.Invoke(CurrentStamina / maxStamina);

        if (CurrentStamina <= 0f && !IsDepleted)
        {
            IsDepleted = true;
            OnStaminaDepleted?.Invoke();
            activeDepletionRoutine = StartCoroutine(HandleDepleted());
        }
    }


    private IEnumerator HandleDepleted()
    {
        Debug.Log("[StaminaSystem] Stamina depleted!");
        StartCoroutine(DelayedDepletionCheck());

        depletionTimer = maxDepletedTime;

        yield return new WaitForSeconds(0.2f);


        if (controller.isWallSticking)
        {
            controller.stickInput = false;

            float nudgeDirection = controller.wallOnRight ? -0.1f : 0.1f;

            controller.velocity = new Vector2(nudgeDirection, 0f);
        }
    }

    private IEnumerator DelayedDepletionCheck()
    {
        yield return new WaitForSeconds(0.15f);

        waitingForLanding = true;
        controller.OnLand += HandleLanding;
    }

    private void HandleLanding()
    {
        if (!waitingForLanding) return;

        waitingForLanding = false;
        controller.OnLand -= HandleLanding;

        activeShatterRoutine = StartCoroutine(ShatterSequence());
    }

    private IEnumerator ShatterSequence()
    {

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("Shatter");
        }

        OnShatterSound?.Invoke();

        yield return new WaitForSeconds(1.33333f);

        controller.transform.position = new Vector2(respawnPosX, respawnPosY);

        controller.rb2d.linearVelocity = Vector2.zero;

        controller.transform.eulerAngles = new Vector3(0f, 0f, 0f);

        OnGlobalReset?.Invoke();

        IsDepleted = false;
        CurrentStamina = maxStamina;
        OnStaminaChanged?.Invoke(CurrentStamina / maxStamina);

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("EndShatter");
        }

        Debug.Log("[StaminaSystem] Shatter sequence complete. Player reset.");
        
        activeShatterRoutine = null;
    }

    public void RefreshStamina()
    {

        if (activeDepletionRoutine != null) StopCoroutine(activeDepletionRoutine);
        if (activeShatterRoutine != null) StopCoroutine(activeShatterRoutine);

        if (waitingForLanding)
        {
            controller.OnLand -= HandleLanding;
            waitingForLanding = false;
        }

        IsDepleted = false;
        CurrentStamina = maxStamina;
        OnStaminaChanged?.Invoke(CurrentStamina / maxStamina);
    }

    public void DepleteAllStamina()
    {
        if (activeShatterRoutine != null) return;

        if (waitingForLanding)
        {
            controller.OnLand -= HandleLanding;
            waitingForLanding = false;
        }

        CurrentStamina = 0f;
        IsDepleted = true;
        OnStaminaChanged?.Invoke(0f);
        OnStaminaDepleted?.Invoke();

        depletionTimer = maxDepletedTime;

        if (activeShatterRoutine != null) StopCoroutine(activeShatterRoutine);
        activeShatterRoutine = StartCoroutine(ShatterSequence());
    }

    private void ForceShatter()
    {
        if (!waitingForLanding) return;

        waitingForLanding = false;
        controller.OnLand -= HandleLanding;

        if (activeShatterRoutine != null) StopCoroutine(activeShatterRoutine);
        activeShatterRoutine = StartCoroutine(ShatterSequence());
    }
}