using UnityEngine;
using System.Collections.Generic;
using Path; // 添加Path命名空间的引用

public class PathPreview : MonoBehaviour
{
    [Header("预览设置")]
    public float lineWidth = PathConstants.PREVIEW_LINE_WIDTH;
    public Color normalColor = new Color(1f, 1f, 1f, PathConstants.PREVIEW_ALPHA);
    public Color highlightColor = new Color(0.5f, 1f, 0.5f, 0.7f);
    
    private LineRenderer lineRenderer;
    private bool isActive = false;
    private PathDataSO currentPath;
    private Vector2 startPosition;
    private List<Vector2> currentTransformedPath;
    
    private void Awake()
    {
        // 创建线渲染器
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = normalColor;
        lineRenderer.endColor = normalColor;
        lineRenderer.enabled = false;
    }
    
    private void Start()
    {
        // 订阅方向输入管理器的事件
        if (DirectionInputManager.Instance != null)
        {
            DirectionInputManager.Instance.OnPreviewPathChanged += OnPreviewPathChanged;
            DirectionInputManager.Instance.OnInputReset += OnInputReset;
        }
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        if (DirectionInputManager.Instance != null)
        {
            DirectionInputManager.Instance.OnPreviewPathChanged -= OnPreviewPathChanged;
            DirectionInputManager.Instance.OnInputReset -= OnInputReset;
        }
    }
    
    private void Update()
    {
        if (!isActive) return;
        
        // 更新预览位置
        UpdatePreviewPosition();
    }
    
    public void StartPreview(PathDataSO pathData, Vector2 position)
    {
        if (pathData == null) return;
        
        Debug.Log("启动路径预览: " + pathData.pathName);
        currentPath = pathData;
        startPosition = position;
        isActive = true;
        lineRenderer.enabled = true;
        
        // 生成路径
        UpdatePreviewPath();
    }
    
    public void StopPreview()
    {
        isActive = false;
        lineRenderer.enabled = false;
        currentPath = null;
        currentTransformedPath = null;
    }
    
    private void UpdatePreviewPosition()
    {
        if (currentPath == null) return;
        
        // 获取鼠标位置
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        // 计算缩放因子
        float distance = Vector2.Distance(startPosition, mousePosition);
        float scale = Mathf.Max(distance / 5f, PathConstants.MIN_SCALE); // 使用最小缩放限制
        scale = Mathf.Min(scale, PathConstants.MAX_SCALE); // 使用最大缩放限制
        
        // 更新路径 - 只应用缩放，不应用旋转
        UpdateTransformedPath(scale);
        
        // 更新线渲染器
        UpdateLineRenderer();
    }
    
    private void UpdatePreviewPath()
    {
        if (currentPath == null) return;
        
        // 获取鼠标位置
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        // 计算缩放因子
        float distance = Vector2.Distance(startPosition, mousePosition);
        float scale = Mathf.Max(distance / 5f, PathConstants.MIN_SCALE); // 使用最小缩放限制
        scale = Mathf.Min(scale, PathConstants.MAX_SCALE); // 使用最大缩放限制
        
        // 更新路径 - 只应用缩放，不应用旋转
        UpdateTransformedPath(scale);
        
        // 更新线渲染器
        UpdateLineRenderer();
    }
    
    private void UpdateTransformedPath(float scale)
    {
        if (currentPath == null) return;
        
        // 获取原始路径点
        List<Vector2> pathPoints = currentPath.GetPathPoints();
        if (pathPoints.Count == 0) return;
        
        // 创建变换后的路径点
        currentTransformedPath = new List<Vector2>();
        
        Vector2 firstPoint = pathPoints[0];
        
        // 确保第一个点是(0,0)，这样它会被转换为startPosition
        if (firstPoint != Vector2.zero)
        {
            Debug.LogWarning($"路径 {currentPath.pathName} 的第一个点不是(0,0)，这可能导致路径起点不在角色位置上");
        }
        
        foreach (Vector2 point in pathPoints)
        {
            // 应用缩放
            Vector2 scaledPoint = point * scale;
            
            // 注意：根据新规则，路径只能缩放，不能旋转
            // 直接应用缩放后的点，不进行旋转
            
            // 应用偏移 - 确保第一个点始终位于startPosition
            Vector2 transformedPoint = scaledPoint + startPosition - (firstPoint * scale);
            currentTransformedPath.Add(transformedPoint);
        }
    }
    
    private void UpdateLineRenderer()
    {
        if (currentTransformedPath == null || currentTransformedPath.Count == 0) return;
        
        // 设置线渲染器的点数
        lineRenderer.positionCount = currentTransformedPath.Count;
        
        // 设置线渲染器的点
        for (int i = 0; i < currentTransformedPath.Count; i++)
        {
            lineRenderer.SetPosition(i, new Vector3(currentTransformedPath[i].x, currentTransformedPath[i].y, 0));
        }
    }
    
    private void OnPreviewPathChanged(PathDataSO pathData)
    {
        // 如果路径为空，停止预览
        if (pathData == null)
        {
            StopPreview();
            return;
        }
        
        // 更新当前路径
        currentPath = pathData;
        
        // 更新预览
        if (isActive)
        {
            UpdatePreviewPath();
        }
        else
        {
            StartPreview(pathData, startPosition);
        }
    }
    
    private void OnInputReset()
    {
        // 重置输入时停止预览
        StopPreview();
    }
    
    public bool IsActive()
    {
        return isActive;
    }
    
    public List<Vector2> GetCurrentTransformedPath()
    {
        return currentTransformedPath;
    }
} 