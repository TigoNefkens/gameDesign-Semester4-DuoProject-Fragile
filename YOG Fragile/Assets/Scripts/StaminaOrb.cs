using UnityEngine;

public class StaminaOrb : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Collider2D orbCollider;
    private bool isCollected = false;

    public GameObject staminaObject;

    private StaminaSystem staminaSystem;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        orbCollider = GetComponent<Collider2D>();

        staminaSystem = staminaObject.GetComponent<StaminaSystem>();
    }

    private void OnEnable()
    {
        // Subscribe to a static event so all orbs hear the reset signal
        StaminaSystem.OnGlobalReset += ResetOrb;
    }

    private void OnDisable()
    {
        StaminaSystem.OnGlobalReset -= ResetOrb;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isCollected && collision.CompareTag("Player") && staminaSystem.activeShatterRoutine == null)
        {
            StaminaSystem playerStamina = collision.GetComponent<StaminaSystem>();
            if (playerStamina != null)
            {
                playerStamina.RefreshStamina();
                Collect();
            }
        }
    }

    private void Collect()
    {
        isCollected = true;
        spriteRenderer.enabled = false;
        orbCollider.enabled = false;
    }

    private void ResetOrb()
    {
        isCollected = false;
        spriteRenderer.enabled = true;
        orbCollider.enabled = true;
    }
}