using UnityEngine;
using DG.Tweening;

public class CameraController : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private Transform target; // Player transform to follow
    [SerializeField] private float followSpeed = 5f; // Camera follow speed (damping)
    [SerializeField] private bool isFollowing = true; // Whether camera is following the target
    
    [Header("Zoom Settings")]
    [SerializeField] private float minZoom = 3f; // Minimum orthographic size
    [SerializeField] private float maxZoom = 10f; // Maximum orthographic size
    [SerializeField] private float defaultZoom = 5f; // Default orthographic size
    [SerializeField] private float zoomSpeed = 1f; // Zoom speed multiplier
    [SerializeField] private float zoomSmoothTime = 0.2f; // Zoom smoothing time
    
    [Header("Pan Settings")]
    [SerializeField] private float panSpeed = 5f; // Camera pan speed when dragging
    [SerializeField] private bool isPanning = false; // Whether camera is being panned
    
    [Header("Reset Animation")]
    [SerializeField] private float resetDuration = 0.5f; // Duration of reset animation
    [SerializeField] private Ease resetEase = Ease.OutQuad; // Easing function for reset animation
    
    private Camera mainCamera;
    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero;
    private float currentZoom;
    private float targetZoom;
    private float zoomVelocity;
    private Vector3 lastMousePosition;
    private Tweener resetTween;
    
    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
        
        // Initialize zoom values
        currentZoom = defaultZoom;
        targetZoom = defaultZoom;
        mainCamera.orthographicSize = currentZoom;
        
        // Find player if target is not set
        if (target == null)
        {
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                target = player.transform;
            }
        }
        
        // Initialize target position
        if (target != null)
        {
            targetPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
            transform.position = targetPosition;
        }
    }
    
    private void LateUpdate()
    {
        HandleZoom();
        HandlePanning();
        HandleFollowing();
        HandleReset();
    }
    
    private void HandleZoom()
    {
        // Get mouse scroll wheel input
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (scrollDelta != 0)
        {
            // Update target zoom level
            targetZoom = Mathf.Clamp(targetZoom - scrollDelta * zoomSpeed, minZoom, maxZoom);
        }
        
        // Smoothly interpolate current zoom to target zoom
        if (currentZoom != targetZoom)
        {
            currentZoom = Mathf.SmoothDamp(currentZoom, targetZoom, ref zoomVelocity, zoomSmoothTime);
            mainCamera.orthographicSize = currentZoom;
        }
    }
    
    private void HandlePanning()
    {
        // Start panning when right mouse button is pressed
        if (Input.GetMouseButtonDown(1))
        {
            isPanning = true;
            isFollowing = false;
            lastMousePosition = Input.mousePosition;
        }
        // Stop panning when right mouse button is released
        else if (Input.GetMouseButtonUp(1))
        {
            isPanning = false;
        }
        
        // Pan camera while right mouse button is held
        if (isPanning)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            Vector3 move = new Vector3(-delta.x, -delta.y, 0) * panSpeed * currentZoom / Screen.width;
            transform.position += move;
            lastMousePosition = Input.mousePosition;
        }
    }
    
    private void HandleFollowing()
    {
        if (!isFollowing || target == null) return;
        
        // Calculate target position (keep camera's z position)
        targetPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
        
        // Smoothly move camera to target position
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, 1f / followSpeed);
    }
    
    private void HandleReset()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ResetCamera();
        }
    }
    
    public void ResetCamera()
    {
        // Kill any existing reset animation
        if (resetTween != null && resetTween.IsActive())
        {
            resetTween.Kill();
        }
        
        // Enable following
        isFollowing = true;
        isPanning = false;
        
        // Reset zoom
        targetZoom = defaultZoom;
        
        // Animate camera position and zoom
        if (target != null)
        {
            Vector3 resetPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
            resetTween = transform.DOMove(resetPosition, resetDuration)
                .SetEase(resetEase);
            
            DOTween.To(() => mainCamera.orthographicSize, x => mainCamera.orthographicSize = x, defaultZoom, resetDuration)
                .SetEase(resetEase);
        }
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null && isFollowing)
        {
            targetPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
        }
    }
    
    public void SetFollowing(bool follow)
    {
        isFollowing = follow;
    }
    
    public void SetZoom(float zoom)
    {
        targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
    }
} 