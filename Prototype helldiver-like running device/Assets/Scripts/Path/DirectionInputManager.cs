using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public class DirectionInputManager : MonoBehaviour
{
    public static DirectionInputManager Instance { get; private set; }
    
    [Header("Input Settings")]
    public float inputCooldown = 0.2f; // Input cooldown time to prevent continuous key presses
    
    [Header("Preview Settings")]
    public float previewCycleTime = 1.0f; // Preview cycle time, switch path every second
    
    // Current direction sequence
    private List<Direction> currentDirectionSequence = new List<Direction>();
    
    // Current matched paths
    private List<PathDataSO> matchedPaths = new List<PathDataSO>();
    
    // Current preview path index
    private int currentPreviewIndex = 0;
    
    // Whether inputting direction
    private bool isInputting = false;
    
    // Input cooldown timer
    private float inputCooldownTimer = 0f;
    
    // Preview cycle timer
    private float previewCycleTimer = 0f;
    
    // Events
    public event Action<List<Direction>> OnDirectionSequenceChanged;
    public event Action<List<PathDataSO>> OnMatchedPathsChanged;
    public event Action<PathDataSO> OnPreviewPathChanged;
    public event Action OnInputReset;
    public event Action<int, int> OnStaminaPreviewChanged; // Parameters: estimated stamina cost, estimated remaining stamina
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Update()
    {
        // Update input cooldown timer
        if (inputCooldownTimer > 0)
        {
            inputCooldownTimer -= Time.deltaTime;
        }
        
        // Handle direction key input
        HandleDirectionInput();
        
        // Handle backspace key
        if (Input.GetKeyDown(KeyCode.Backspace) && currentDirectionSequence.Count > 0)
        {
            RemoveLastDirection();
        }
        
        // Handle preview cycle
        if (matchedPaths.Count > 0)
        {
            previewCycleTimer += Time.deltaTime;
            if (previewCycleTimer >= previewCycleTime)
            {
                previewCycleTimer = 0f;
                CyclePreviewPath();
            }
        }
        
        // Handle confirmation selection - only when there are matched paths
        if (Input.GetMouseButtonDown(0))
        {
            // If there are matched paths, execute the path
            if (matchedPaths.Count > 0)
            {
                Debug.Log("Mouse click detected, matched paths count: " + matchedPaths.Count);
                
                // Check if there is enough stamina
                if (StaminaManager.Instance != null && 
                    !StaminaManager.Instance.HasEnoughStamina(currentDirectionSequence.Count))
                {
                    // Insufficient stamina, notify stamina manager
                    if (StaminaManager.Instance != null)
                    {
                        // This will trigger the UI shake animation
                        Debug.Log("Insufficient stamina for path execution");
                    }
                    return;
                }
                
                SelectRandomPath();
            }
            else
            {
                // If there are no matched paths, consume this click event
                Debug.Log("Mouse click detected, but no matched paths, ignoring this click");
                // Ensure this click won't be incorrectly applied in subsequent operations
                ConsumeMouseClick();
            }
        }
    }
    
    private void HandleDirectionInput()
    {
        if (inputCooldownTimer <= 0)
        {
            Direction? newDirection = null;
            
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                newDirection = Direction.Up;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                newDirection = Direction.Right;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                newDirection = Direction.Down;
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                newDirection = Direction.Left;
            }
            
            if (newDirection.HasValue)
            {
                // Check if there is enough stamina after adding a new direction
                if (StaminaManager.Instance != null && 
                    !StaminaManager.Instance.HasEnoughStamina(currentDirectionSequence.Count + 1))
                {
                    // Insufficient stamina, notify stamina manager
                    if (StaminaManager.Instance != null)
                    {
                        // This will trigger the UI shake animation
                        Debug.Log("Insufficient stamina for adding direction");
                    }
                    return;
                }
                
                AddDirection(newDirection.Value);
            }
        }
    }
    
    private void AddDirection(Direction direction)
    {
        currentDirectionSequence.Add(direction);
        inputCooldownTimer = inputCooldown;
        isInputting = true;
        
        // Update matched paths
        UpdateMatchedPaths();
        
        // Update stamina preview
        UpdateStaminaPreview();
        
        // Trigger event
        OnDirectionSequenceChanged?.Invoke(currentDirectionSequence);
    }
    
    private void RemoveLastDirection()
    {
        if (currentDirectionSequence.Count > 0)
        {
            currentDirectionSequence.RemoveAt(currentDirectionSequence.Count - 1);
            inputCooldownTimer = inputCooldown;
            
            // If the sequence is empty, reset input state
            if (currentDirectionSequence.Count == 0)
            {
                ResetInput();
            }
            else
            {
                // Update matched paths
                UpdateMatchedPaths();
                
                // Update stamina preview
                UpdateStaminaPreview();
                
                // Trigger event
                OnDirectionSequenceChanged?.Invoke(currentDirectionSequence);
                
                // Ensure that previous clicks won't be incorrectly applied after removing a direction
                ConsumeMouseClick();
            }
        }
    }
    
    private void UpdateMatchedPaths()
    {
        // Save the previous matched path count
        int previousMatchCount = matchedPaths.Count;
        
        matchedPaths.Clear();
        currentPreviewIndex = 0;
        previewCycleTimer = 0f;
        
        // Get all unlocked paths
        List<PathDataSO> unlockedPaths = PlayerPathInventory.Instance.GetUnlockedPaths();
        
        // Filter matched paths
        foreach (PathDataSO path in unlockedPaths)
        {
            if (IsPathMatched(path))
            {
                matchedPaths.Add(path);
            }
        }
        
        // Trigger event
        OnMatchedPathsChanged?.Invoke(matchedPaths);
        
        // Update preview path - only trigger preview when there are matched paths
        if (matchedPaths.Count > 0)
        {
            OnPreviewPathChanged?.Invoke(matchedPaths[currentPreviewIndex]);
            
            // If there were no matched paths before, but now there are, ensure previous clicks won't be incorrectly applied
            if (previousMatchCount == 0)
            {
                ConsumeMouseClick();
            }
        }
        else
        {
            // If there are no matched paths, trigger reset preview
            OnInputReset?.Invoke();
        }
    }
    
    private void UpdateStaminaPreview()
    {
        if (StaminaManager.Instance != null && currentDirectionSequence.Count > 0)
        {
            int cost = StaminaManager.Instance.PreviewStaminaCost(currentDirectionSequence.Count);
            int remaining = StaminaManager.Instance.PreviewRemainingStamina(currentDirectionSequence.Count);
            OnStaminaPreviewChanged?.Invoke(cost, remaining);
        }
    }
    
    private bool IsPathMatched(PathDataSO path)
    {
        // Check if the path's direction sequence starts with the current input sequence
        if (path.directionSequence.Count < currentDirectionSequence.Count)
        {
            return false;
        }
        
        for (int i = 0; i < currentDirectionSequence.Count; i++)
        {
            if (path.directionSequence[i] != currentDirectionSequence[i])
            {
                return false;
            }
        }
        
        return true;
    }
    
    private void CyclePreviewPath()
    {
        if (matchedPaths.Count > 0)
        {
            currentPreviewIndex = (currentPreviewIndex + 1) % matchedPaths.Count;
            OnPreviewPathChanged?.Invoke(matchedPaths[currentPreviewIndex]);
        }
    }
    
    private void SelectRandomPath()
    {
        if (matchedPaths.Count > 0)
        {
            // Randomly select a path
            int randomIndex = UnityEngine.Random.Range(0, matchedPaths.Count);
            PathDataSO selectedPath = matchedPaths[randomIndex];
            
            Debug.Log("Randomly selected path: " + selectedPath.pathName + ", preparing to execute");
            
            // Notify the path selection manager and ensure the path is applied
            if (PathSelectionManager.Instance != null)
            {
                // First select the path, trigger preview
                PathSelectionManager.Instance.SelectPath(selectedPath);
                
                // Then notify PlayerController to execute the path
                PlayerController playerController = FindObjectOfType<PlayerController>();
                if (playerController != null)
                {
                    Debug.Log("Notifying PlayerController to execute the path");
                    // Use public method to execute the path
                    playerController.ExecuteSelectedPath();
                }
                
                // Delay reset input
                StartCoroutine(DelayedResetInput());
            }
            else
            {
                // If there is no PathSelectionManager, reset input directly
                ResetInput();
            }
        }
    }
    
    private IEnumerator DelayedResetInput()
    {
        // Wait for a while to ensure path selection and execution events are processed
        yield return new WaitForSeconds(0.1f);
        
        // Reset input
        ResetInput();
    }
    
    public void ResetInput()
    {
        currentDirectionSequence.Clear();
        matchedPaths.Clear();
        currentPreviewIndex = 0;
        previewCycleTimer = 0f;
        isInputting = false;
        
        // Trigger event
        OnInputReset?.Invoke();
    }
    
    public List<Direction> GetCurrentDirectionSequence()
    {
        return new List<Direction>(currentDirectionSequence);
    }
    
    public List<PathDataSO> GetMatchedPaths()
    {
        return new List<PathDataSO>(matchedPaths);
    }
    
    public PathDataSO GetCurrentPreviewPath()
    {
        if (matchedPaths.Count > 0)
        {
            return matchedPaths[currentPreviewIndex];
        }
        return null;
    }
    
    public bool IsInputting()
    {
        return isInputting;
    }
    
    // Add a method to consume mouse click events
    private void ConsumeMouseClick()
    {
        // Notify PlayerController to consume the mouse click event
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerController.ConsumeMouseClick();
        }
    }
} 