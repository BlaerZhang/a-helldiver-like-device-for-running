using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class StaminaDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider staminaSlider; // Current stamina slider
    [SerializeField] private Slider previewSlider; // Preview stamina slider
    [SerializeField] private TextMeshProUGUI staminaText;
    
    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color warningColor = new Color(0.8f, 0.8f, 0.2f);
    [SerializeField] private Color dangerColor = new Color(0.8f, 0.2f, 0.2f);
    [SerializeField] private Color previewColor = new Color(0.5f, 0.5f, 0.5f, 0.7f); // Preview color
    [SerializeField] private float warningThreshold = 0.5f; // 50%
    [SerializeField] private float dangerThreshold = 0.25f; // 25%
    
    [Header("Animation Settings")]
    [SerializeField] private float previewAnimDuration = 0.3f;
    [SerializeField] private float shakeStrength = 5f;
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private int shakeVibrato = 10;
    [SerializeField] private float shakeRandomness = 90f;
    
    private Image sliderFillImage;
    private Image previewFillImage;
    private RectTransform rectTransform;
    private Tweener shakeTween;
    private Tweener previewTween;
    
    private void Awake()
    {
        // Get the fill image of the main slider
        if (staminaSlider != null)
        {
            sliderFillImage = staminaSlider.fillRect.GetComponent<Image>();
        }
        
        // Get the fill image of the preview slider
        if (previewSlider != null)
        {
            previewFillImage = previewSlider.fillRect.GetComponent<Image>();
            previewFillImage.color = previewColor;
        }
        
        rectTransform = GetComponent<RectTransform>();
    }
    
    private void Start()
    {
        // Initialize preview slider
        if (previewSlider != null)
        {
            previewSlider.value = 0f;
        }
        
        // Subscribe to stamina manager events
        if (StaminaManager.Instance != null)
        {
            StaminaManager.Instance.OnStaminaChanged += UpdateStaminaDisplay;
            StaminaManager.Instance.OnStaminaInsufficient += PlayShakeAnimation;
            
            // Initialize display
            UpdateStaminaDisplay(StaminaManager.Instance.GetCurrentStamina(), StaminaManager.Instance.GetMaxStamina());
        }
        
        // Subscribe to direction input manager events
        if (DirectionInputManager.Instance != null)
        {
            DirectionInputManager.Instance.OnStaminaPreviewChanged += UpdateCostPreview;
            DirectionInputManager.Instance.OnInputReset += ResetCostPreview;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (StaminaManager.Instance != null)
        {
            StaminaManager.Instance.OnStaminaChanged -= UpdateStaminaDisplay;
            StaminaManager.Instance.OnStaminaInsufficient -= PlayShakeAnimation;
        }
        
        if (DirectionInputManager.Instance != null)
        {
            DirectionInputManager.Instance.OnStaminaPreviewChanged -= UpdateCostPreview;
            DirectionInputManager.Instance.OnInputReset -= ResetCostPreview;
        }
        
        // Stop all animations
        if (shakeTween != null && shakeTween.IsActive())
        {
            shakeTween.Kill();
        }
        
        if (previewTween != null && previewTween.IsActive())
        {
            previewTween.Kill();
        }
    }
    
    private void UpdateStaminaDisplay(int current, int max)
    {
        // Update main slider
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = max;
            staminaSlider.value = current;
            
            // Update color
            if (sliderFillImage != null)
            {
                float ratio = (float)current / max;
                if (ratio <= dangerThreshold)
                {
                    sliderFillImage.color = dangerColor;
                }
                else if (ratio <= warningThreshold)
                {
                    sliderFillImage.color = warningColor;
                }
                else
                {
                    sliderFillImage.color = normalColor;
                }
            }
        }
        
        // Update preview slider to match current value
        if (previewSlider != null)
        {
            previewSlider.maxValue = max;
            previewSlider.value = current;
        }
        
        // Update text
        if (staminaText != null)
        {
            staminaText.text = $"STAMINA: {current} / {max}";
        }
    }
    
    private void UpdateCostPreview(int cost, int remaining)
    {
        if (StaminaManager.Instance == null) return;
        
        int current = StaminaManager.Instance.GetCurrentStamina();
        int max = StaminaManager.Instance.GetMaxStamina();
        
        // Update preview slider
        if (previewSlider != null)
        {
            // Stop previous animation
            if (previewTween != null && previewTween.IsActive())
            {
                previewTween.Kill();
            }
            
            // Animate preview slider to show remaining stamina
            previewTween = DOTween.To(() => previewSlider.value, x => previewSlider.value = x, remaining, previewAnimDuration)
                .SetEase(Ease.OutQuad);
        }
        
        // Update text
        if (staminaText != null)
        {
            staminaText.text = $"STAMINA: {current} / {max} (-{cost})";
        }
        
        // If stamina is insufficient, play shake animation
        if (remaining <= 0)
        {
            PlayShakeAnimation();
        }
    }
    
    private void ResetCostPreview()
    {
        // Stop previous animation
        if (previewTween != null && previewTween.IsActive())
        {
            previewTween.Kill();
        }
        
        // Reset preview slider to match current value
        if (previewSlider != null && StaminaManager.Instance != null)
        {
            previewSlider.value = StaminaManager.Instance.GetCurrentStamina();
        }
        
        // Reset text
        if (staminaText != null && StaminaManager.Instance != null)
        {
            int current = StaminaManager.Instance.GetCurrentStamina();
            int max = StaminaManager.Instance.GetMaxStamina();
            staminaText.text = $"STAMINA: {current} / {max}";
        }
    }
    
    private void PlayShakeAnimation()
    {
        // Stop previous shake animation
        if (shakeTween != null && shakeTween.IsActive())
        {
            shakeTween.Kill();
        }
        
        // Play shake animation
        shakeTween = rectTransform.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, shakeRandomness)
            .SetEase(Ease.OutQuad);
        
        // Flash color
        if (sliderFillImage != null)
        {
            Color originalColor = sliderFillImage.color;
            sliderFillImage.DOColor(dangerColor, shakeDuration * 0.5f)
                .SetLoops(2, LoopType.Yoyo)
                .OnComplete(() => sliderFillImage.color = originalColor);
        }
    }
} 