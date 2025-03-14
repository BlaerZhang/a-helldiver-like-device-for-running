using UnityEngine;
using System.Collections.Generic;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerPathInventory : MonoBehaviour
{
    public static PlayerPathInventory Instance { get; private set; }
    
    [Header("Initial Paths")]
    [SerializeField]
    private List<PathDataSO> startingPaths = new List<PathDataSO>(); // 玩家开始时拥有的路径
    
    [Header("Current Paths")]
    [SerializeField]
    private List<PathDataSO> currentPaths = new List<PathDataSO>(); // 当前拥有的路径，可在Inspector中修改
    
    private HashSet<PathDataSO> unlockedPaths = new HashSet<PathDataSO>();
    public event Action OnInventoryChanged;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeInventory();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnEnable()
    {
        if (Instance == this)
        {
            InitializeInventory();
        }
    }
    
    private void InitializeInventory()
    {
        unlockedPaths.Clear();
        currentPaths.Clear();
        
        // 添加初始路径
        if (startingPaths != null)
        {
            foreach (var path in startingPaths)
            {
                if (path != null)
                {
                    unlockedPaths.Add(path);
                    currentPaths.Add(path);
                }
            }
        }
        NotifyInventoryChanged();
    }
    
    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        
        // 将currentPaths的变化同步到unlockedPaths
        unlockedPaths.Clear();
        foreach (var path in currentPaths)
        {
            if (path != null)
            {
                unlockedPaths.Add(path);
            }
        }
        NotifyInventoryChanged();
    }
    
    public void UnlockPath(PathDataSO path)
    {
        if (path != null && !unlockedPaths.Contains(path))
        {
            unlockedPaths.Add(path);
            currentPaths.Add(path);
            NotifyInventoryChanged();
        }
    }
    
    public void LockPath(PathDataSO path)
    {
        if (path != null && unlockedPaths.Contains(path))
        {
            unlockedPaths.Remove(path);
            currentPaths.Remove(path);
            NotifyInventoryChanged();
        }
    }
    
    public bool HasPath(PathDataSO path)
    {
        return path != null && unlockedPaths.Contains(path);
    }
    
    public List<PathDataSO> GetUnlockedPaths()
    {
        return new List<PathDataSO>(currentPaths);
    }
    
    private void NotifyInventoryChanged()
    {
        try
        {
            OnInventoryChanged?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in inventory change notification: {e.Message}");
        }
    }
    
#if UNITY_EDITOR
    // 添加编辑器功能来快速添加/移除路径
    public void EditorAddPath(PathDataSO path)
    {
        if (!currentPaths.Contains(path))
        {
            currentPaths.Add(path);
            EditorUtility.SetDirty(this);
        }
    }
    
    public void EditorRemovePath(PathDataSO path)
    {
        if (currentPaths.Contains(path))
        {
            currentPaths.Remove(path);
            EditorUtility.SetDirty(this);
        }
    }
#endif
    
    // 用于保存数据
    public void SaveInventory()
    {
        // TODO: 实现存档功能
    }
    
    // 用于加载数据
    public void LoadInventory()
    {
        // TODO: 实现读档功能
        NotifyInventoryChanged();
    }
} 