using UnityEngine;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player GameObject that has StaminaSystem attached")]
    public GameObject player;

    [Tooltip("The Image used as the stamina fill (Image Type = Filled, Horizontal)")]
    public Image fillImage;

    [Header("Colours")]
    public Color normalColor = new Color(0.20f, 0.78f, 0.35f);
    public Color warningColor = new Color(1.00f, 0.75f, 0.00f);
    public Color criticalColor = new Color(0.90f, 0.15f, 0.15f);

    [Header("Scale")]
    [Tooltip("The default X scale of the fill rect at full stamina (match your RectTransform)")]
    public float baseScaleX = 1f;

    [Tooltip("Stamina fraction below which the bar turns amber")]
    [Range(0f, 1f)]
    public float warningThreshold = 0.35f;

    [Tooltip("Stamina fraction below which the bar turns red")]
    [Range(0f, 1f)]
    public float criticalThreshold = 0.15f;

    private StaminaSystem staminaSystem;
    private float currentFill = 1f;

    private void Start()
    {
        if (player == null)
        {
            Debug.LogWarning("[StaminaBar] Player reference not assigned - searching scene.");
            var found = FindFirstObjectByType<StaminaSystem>();
            if (found != null) staminaSystem = found;
        }
        else
        {
            staminaSystem = player.GetComponent<StaminaSystem>();
        }

        if (staminaSystem == null)
        {
            Debug.LogError("[StaminaBar] Could not find StaminaSystem on player!");
            enabled = false;
            return;
        }

        currentFill = staminaSystem.CurrentStamina / staminaSystem.maxStamina;
        staminaSystem.OnStaminaChanged += OnStaminaChanged;

        RefreshFill();
    }

    private void OnDestroy()
    {
        if (staminaSystem != null)
            staminaSystem.OnStaminaChanged -= OnStaminaChanged;
    }

    private void OnStaminaChanged(float normalised)
    {
        float oldFill = currentFill;
        currentFill = normalised;
        RefreshFill();
    }

    private void RefreshFill()
    {
        if (fillImage == null) return;

        fillImage.fillAmount = currentFill;

        Vector3 scale = fillImage.rectTransform.localScale;
        scale.x = currentFill * baseScaleX;
        fillImage.rectTransform.localScale = scale;

        if (currentFill <= criticalThreshold)
            fillImage.color = criticalColor;
        else if (currentFill <= warningThreshold)
            fillImage.color = warningColor;
        else
            fillImage.color = normalColor;
    }
}