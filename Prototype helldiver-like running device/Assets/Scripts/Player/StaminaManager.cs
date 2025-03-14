using UnityEngine;
using System;

public class StaminaManager : MonoBehaviour
{
    [Header("Stamina Settings")]
    [SerializeField] private int maxStamina = 100;
    [SerializeField] private int currentStamina = 100;
    [SerializeField] private int baseStaminaCost = 5; // Base stamina cost
    
    // Events
    public event Action<int, int> OnStaminaChanged; // Parameters: current stamina, max stamina
    public event Action OnStaminaInsufficient; // Triggered when stamina is insufficient
    
    // Singleton
    public static StaminaManager Instance { get; private set; }
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Ensure not destroyed on scene change
        DontDestroyOnLoad(gameObject);
        
        // Initialize stamina value
        currentStamina = maxStamina;
    }
    
    private void Start()
    {
        // Subscribe to path execution event
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnPathExecuted += OnPathExecuted;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnPathExecuted -= OnPathExecuted;
        }
    }
    
    // Calculate stamina cost for executing a path
    public int CalculateStaminaCost(int directionCount)
    {
        return baseStaminaCost + directionCount;
    }
    
    // Check if there is enough stamina to execute a path
    public bool HasEnoughStamina(int directionCount)
    {
        int cost = CalculateStaminaCost(directionCount);
        return currentStamina >= cost;
    }
    
    // Preview stamina cost
    public int PreviewStaminaCost(int directionCount)
    {
        return CalculateStaminaCost(directionCount);
    }
    
    // Preview remaining stamina after execution
    public int PreviewRemainingStamina(int directionCount)
    {
        int cost = CalculateStaminaCost(directionCount);
        return Mathf.Max(0, currentStamina - cost);
    }
    
    // Consume stamina
    public bool ConsumeStamina(int directionCount)
    {
        int cost = CalculateStaminaCost(directionCount);
        
        if (currentStamina < cost)
        {
            // Insufficient stamina
            OnStaminaInsufficient?.Invoke();
            return false;
        }
        
        // Deduct stamina
        currentStamina -= cost;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        return true;
    }
    
    // Restore stamina
    public void RestoreStamina(int amount)
    {
        currentStamina = Mathf.Min(currentStamina + amount, maxStamina);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }
    
    // Get current stamina
    public int GetCurrentStamina()
    {
        return currentStamina;
    }
    
    // Get max stamina
    public int GetMaxStamina()
    {
        return maxStamina;
    }
    
    // Callback when path is executed
    private void OnPathExecuted(PathDataSO pathData)
    {
        if (pathData != null)
        {
            ConsumeStamina(pathData.directionSequence.Count);
        }
    }
} 