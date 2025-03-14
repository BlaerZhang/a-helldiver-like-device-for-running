using UnityEngine;
using System;
using System.Collections.Generic;

public class PathSelectionManager : MonoBehaviour
{
    public static PathSelectionManager Instance { get; private set; }
    
    // 当前选中的路径
    private PathDataSO currentPath;
    
    // 事件
    public event Action<PathDataSO> OnPathSelected;
    
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
    
    private void Start()
    {
        // 订阅方向输入管理器的事件
        if (DirectionInputManager.Instance != null)
        {
            DirectionInputManager.Instance.OnPreviewPathChanged += OnPreviewPathChanged;
        }
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        if (DirectionInputManager.Instance != null)
        {
            DirectionInputManager.Instance.OnPreviewPathChanged -= OnPreviewPathChanged;
        }
    }
    
    public void SelectPath(PathDataSO path)
    {
        // 如果路径为空，不进行任何操作
        if (path == null) return;
        
        Debug.Log("PathSelectionManager选择路径: " + path.pathName);
        currentPath = path;
        OnPathSelected?.Invoke(path);
    }
    
    private void OnPreviewPathChanged(PathDataSO path)
    {
        // 更新预览路径
        if (path != null)
        {
            // 只更新预览，不执行路径
            OnPathSelected?.Invoke(path);
        }
    }
    
    public PathDataSO GetCurrentPath()
    {
        return currentPath;
    }
} 