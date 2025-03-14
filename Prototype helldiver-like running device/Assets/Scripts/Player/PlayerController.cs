using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using FogOfWar;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    [Range(0f, 1f)]
    public float oldPathAlpha = 0.3f; // 已走过路径的透明度
    
    private PathPreview pathPreview;
    private List<LineRenderer> pathTrails = new List<LineRenderer>();
    private bool isMoving = false;
    private List<Vector2> currentPathPoints;
    private int currentPathIndex = 0;
    private PathDataSO currentExecutingPath; // 当前正在执行的路径
    
    // 添加一个变量来记录最后一次鼠标点击的时间
    private float lastMouseClickTime = 0f;
    // 添加一个变量来标记是否应该忽略下一次鼠标点击
    private bool ignoreNextMouseClick = false;
    
    // 添加事件
    public event Action<PathDataSO> OnPathExecuted; // 路径执行完成时触发
    
    // 单例
    public static PlayerController Instance { get; private set; }
    
    // 添加一个变量来存储最后执行的路径点
    private List<Vector2> lastExecutedPathPoints = new List<Vector2>();
    
    private void Awake()
    {
        // 单例模式
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    private void Start()
    {
        // 创建路径预览组件
        GameObject previewObj = new GameObject("PathPreview");
        pathPreview = previewObj.AddComponent<PathPreview>();
        
        // 订阅路径选择事件
        if (PathSelectionManager.Instance != null)
        {
            PathSelectionManager.Instance.OnPathSelected += OnPathSelected;
        }
    }
    
    private void OnDestroy()
    {
        if (PathSelectionManager.Instance != null)
        {
            PathSelectionManager.Instance.OnPathSelected -= OnPathSelected;
        }
    }
    
    private void Update()
    {
        if (isMoving)
        {
            MoveAlongPath();
            return;
        }
        
        // 记录鼠标点击时间，但如果需要忽略点击则不记录
        if (Input.GetMouseButtonDown(0))
        {
            if (ignoreNextMouseClick)
            {
                // 如果需要忽略这次点击，重置标记
                Debug.Log("忽略这次鼠标点击");
                ignoreNextMouseClick = false;
            }
            else
            {
                lastMouseClickTime = Time.time;
                
                // 处理路径预览 - 确保只有在预览激活且有有效路径点时才能执行路径
                if (pathPreview.IsActive())
                {
                    currentPathPoints = pathPreview.GetCurrentTransformedPath();
                    if (currentPathPoints != null && currentPathPoints.Count > 0)
                    {
                        StartMoving();
                    }
                }
            }
        }
    }
    
    private void OnPathSelected(PathDataSO pathData)
    {
        if (pathData == null || isMoving) return;
        
        Debug.Log("路径被选择: " + pathData.pathName);
        currentExecutingPath = pathData; // 记录当前选择的路径
        
        // 启动预览
        pathPreview.StartPreview(pathData, transform.position);
        
        // 检查是否是由DirectionInputManager的SelectRandomPath触发的
        if (DirectionInputManager.Instance != null && 
            DirectionInputManager.Instance.GetMatchedPaths().Count > 0)
        {
            // 检查是否最近有鼠标点击
            if (Time.time - lastMouseClickTime < 0.5f)
            {
                Debug.Log("检测到最近的鼠标点击，尝试自动执行路径");
                // 获取当前变换后的路径点
                currentPathPoints = pathPreview.GetCurrentTransformedPath();
                if (currentPathPoints != null && currentPathPoints.Count > 0)
                {
                    // 延迟一帧执行，确保路径预览已更新
                    StartCoroutine(DelayedStartMoving());
                }
            }
        }
    }
    
    private IEnumerator DelayedStartMoving()
    {
        // 等待一帧，确保路径预览已更新
        yield return null;
        
        // 获取最新的路径点
        currentPathPoints = pathPreview.GetCurrentTransformedPath();
        if (currentPathPoints != null && currentPathPoints.Count > 0)
        {
            StartMoving();
        }
    }
    
    private void StartMoving()
    {
        Debug.Log("开始沿路径移动");
        isMoving = true;
        currentPathIndex = 0;
        pathPreview.StopPreview();
        
        // 保存最后执行的路径点
        lastExecutedPathPoints = new List<Vector2>(currentPathPoints);
        
        // 创建新的路径轨迹渲染器
        GameObject trailObj = new GameObject($"PathTrail_{pathTrails.Count}");
        trailObj.transform.SetParent(transform.parent);
        LineRenderer newTrail = trailObj.AddComponent<LineRenderer>();
        newTrail.material = new Material(Shader.Find("Sprites/Default"));
        
        // 设置半透明的白色
        Color trailColor = Color.white;
        trailColor.a = oldPathAlpha;
        newTrail.startColor = trailColor;
        newTrail.endColor = trailColor;
        
        newTrail.startWidth = 0.1f;
        newTrail.endWidth = 0.1f;
        newTrail.positionCount = currentPathPoints.Count;
        
        // 设置路径点
        for (int i = 0; i < currentPathPoints.Count; i++)
        {
            newTrail.SetPosition(i, new Vector3(currentPathPoints[i].x, currentPathPoints[i].y, 0));
        }
        
        pathTrails.Add(newTrail);
        
        // 触发路径执行事件
        OnPathExecuted?.Invoke(currentExecutingPath);
    }
    
    private void MoveAlongPath()
    {
        if (currentPathIndex >= currentPathPoints.Count - 1)
        {
            isMoving = false;
            return;
        }
        
        Vector2 targetPos = currentPathPoints[currentPathIndex + 1];
        Vector2 currentPos = transform.position;
        
        float step = moveSpeed * Time.deltaTime;
        transform.position = Vector2.MoveTowards(currentPos, targetPos, step);
        
        // 当玩家移动时，探出迷雾
        if (FogOfWarManager.Instance != null)
        {
            FogOfWarManager.Instance.RevealArea(transform.position);
        }
        
        if (Vector2.Distance(transform.position, targetPos) < 0.01f)
        {
            currentPathIndex++;
        }
    }
    
    // 添加一个公开的方法来执行路径
    public void ExecuteSelectedPath()
    {
        Debug.Log("执行选中的路径");
        if (isMoving) return;
        
        // 获取当前变换后的路径点
        currentPathPoints = pathPreview.GetCurrentTransformedPath();
        if (currentPathPoints != null && currentPathPoints.Count > 0)
        {
            StartMoving();
        }
    }
    
    // 添加一个公开的方法来消耗掉鼠标点击事件
    public void ConsumeMouseClick()
    {
        Debug.Log("消耗掉鼠标点击事件");
        ignoreNextMouseClick = true;
    }
    
    // 添加一个公开的方法来获取最后执行的路径点
    public List<Vector2> GetLastExecutedPathPoints()
    {
        return lastExecutedPathPoints;
    }
} 