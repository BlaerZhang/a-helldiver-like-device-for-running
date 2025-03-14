using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Collections;

public class PathInventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform inventoryPanel;
    public GameObject pathSlotPrefab;
    public TextMeshProUGUI directionSequenceText;
    
    [Header("Layout Settings")]
    public float spacing = 10f; // 槽位之间的间距
    public Vector2 slotSize = new Vector2(80, 80);
    public float topOffset = 20f; // 距离屏幕顶部的偏移
    public float horizontalPadding = 20f; // 面板的水平内边距
    
    [Header("Visual Settings")]
    public Color normalColor = new Color(1f, 1f, 1f, 0.5f);
    public Color highlightColor = new Color(1f, 1f, 1f, 1f);
    public Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    
    private List<PathSlot> pathSlots = new List<PathSlot>();
    private Dictionary<PathDataSO, PathSlot> pathSlotMap = new Dictionary<PathDataSO, PathSlot>();
    private GridLayoutGroup gridLayout;
    
    private void Awake()
    {
        SetupPanelLayout();
    }
    
    private void OnEnable()
    {
        // 在启用时订阅事件
        if (PlayerPathInventory.Instance != null)
        {
            PlayerPathInventory.Instance.OnInventoryChanged += RefreshInventory;
        }
        
        // 订阅方向输入管理器的事件
        if (DirectionInputManager.Instance != null)
        {
            DirectionInputManager.Instance.OnDirectionSequenceChanged += OnDirectionSequenceChanged;
            DirectionInputManager.Instance.OnMatchedPathsChanged += OnMatchedPathsChanged;
            DirectionInputManager.Instance.OnPreviewPathChanged += OnPreviewPathChanged;
            DirectionInputManager.Instance.OnInputReset += OnInputReset;
        }
        
        // 如果实例还没准备好，等待一帧后重试
        StartCoroutine(TrySubscribeNextFrame());
    }
    
    private System.Collections.IEnumerator TrySubscribeNextFrame()
    {
        yield return null;
        
        if (PlayerPathInventory.Instance != null)
        {
            PlayerPathInventory.Instance.OnInventoryChanged += RefreshInventory;
            RefreshInventory();
        }
        
        if (DirectionInputManager.Instance != null)
        {
            DirectionInputManager.Instance.OnDirectionSequenceChanged += OnDirectionSequenceChanged;
            DirectionInputManager.Instance.OnMatchedPathsChanged += OnMatchedPathsChanged;
            DirectionInputManager.Instance.OnPreviewPathChanged += OnPreviewPathChanged;
            DirectionInputManager.Instance.OnInputReset += OnInputReset;
        }
    }
    
    private void OnDisable()
    {
        // 在禁用时取消订阅事件
        if (PlayerPathInventory.Instance != null)
        {
            PlayerPathInventory.Instance.OnInventoryChanged -= RefreshInventory;
        }
        
        if (DirectionInputManager.Instance != null)
        {
            DirectionInputManager.Instance.OnDirectionSequenceChanged -= OnDirectionSequenceChanged;
            DirectionInputManager.Instance.OnMatchedPathsChanged -= OnMatchedPathsChanged;
            DirectionInputManager.Instance.OnPreviewPathChanged -= OnPreviewPathChanged;
            DirectionInputManager.Instance.OnInputReset -= OnInputReset;
        }
    }
    
    private void SetupPanelLayout()
    {
        if (inventoryPanel == null) return;
        
        // 获取并配置或添加GridLayoutGroup组件
        gridLayout = inventoryPanel.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            // 移除其他可能存在的布局组件
            var horizontalLayout = inventoryPanel.GetComponent<HorizontalLayoutGroup>();
            if (horizontalLayout != null)
            {
                Destroy(horizontalLayout);
            }
            var verticalLayout = inventoryPanel.GetComponent<VerticalLayoutGroup>();
            if (verticalLayout != null)
            {
                Destroy(verticalLayout);
            }
            
            // 添加GridLayoutGroup
            gridLayout = inventoryPanel.gameObject.AddComponent<GridLayoutGroup>();
        }
        
        // 配置GridLayoutGroup
        gridLayout.cellSize = new Vector2(slotSize.x, slotSize.y);
        gridLayout.spacing = new Vector2(spacing, spacing);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 17;
        
        // 获取或添加ContentSizeFitter
        var fitter = inventoryPanel.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = inventoryPanel.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        // 设置RectTransform
        inventoryPanel.anchorMin = new Vector2(0.5f, 1f);
        inventoryPanel.anchorMax = new Vector2(0.5f, 1f);
        inventoryPanel.pivot = new Vector2(0.5f, 1f);
        
        // 设置最大宽度为屏幕宽度的90%
        float maxWidth = Screen.width * 0.9f;
        int columnsCount = Mathf.Max(1, Mathf.FloorToInt(maxWidth / (slotSize.x + spacing)));
        float actualWidth = columnsCount * (slotSize.x + spacing) - spacing;
        inventoryPanel.sizeDelta = new Vector2(actualWidth, 0); // 高度会由ContentSizeFitter自动调整
        
        Debug.Log("设置面板布局完成");
    }
    
    private void Start()
    {
        // 在Start中再次确保面板位置正确
        SetupPanelLayout();
        
        // 延迟一帧刷新库存，确保所有组件都已初始化
        StartCoroutine(DelayedRefresh());
    }
    
    private IEnumerator DelayedRefresh()
    {
        yield return new WaitForEndOfFrame();
        RefreshInventory();
    }
    
    public void RefreshInventory()
    {
        if (inventoryPanel == null) return;
        
        // 清理现有的槽位
        foreach (PathSlot slot in pathSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        pathSlots.Clear();
        pathSlotMap.Clear();
        
        // 获取玩家已解锁的路径
        List<PathDataSO> unlockedPaths = PlayerPathInventory.Instance?.GetUnlockedPaths() ?? new List<PathDataSO>();
        
        if (unlockedPaths.Count == 0) return;
        
        // 为每个已解锁的路径创建槽位
        foreach (PathDataSO pathData in unlockedPaths)
        {
            CreatePathSlot(pathData);
        }
        
        // 如果有方向输入管理器，更新匹配状态
        if (DirectionInputManager.Instance != null && DirectionInputManager.Instance.IsInputting())
        {
            OnMatchedPathsChanged(DirectionInputManager.Instance.GetMatchedPaths());
        }
        
        // 强制布局刷新
        LayoutRebuilder.ForceRebuildLayoutImmediate(inventoryPanel);
        
        // 调整面板位置
        Vector2 position = inventoryPanel.anchoredPosition;
        position.y = -topOffset; // 距离顶部的偏移
        inventoryPanel.anchoredPosition = position;
    }
    
    private void CreatePathSlot(PathDataSO pathData)
    {
        if (pathData == null) return;
        
        // 创建槽位对象
        GameObject slotObj = Instantiate(pathSlotPrefab, inventoryPanel);
        PathSlot pathSlot = slotObj.GetComponent<PathSlot>();
        pathSlots.Add(pathSlot);
        pathSlotMap[pathData] = pathSlot;
        
        // 设置槽位大小
        RectTransform slotRect = slotObj.GetComponent<RectTransform>();
        slotRect.sizeDelta = slotSize;
        
        // 生成路径图标
        PathIconGenerator.GeneratePathIcon(pathData, pathSlot.iconImage);
        
        // 设置路径数据
        pathSlot.SetPathData(pathData);
        
        // 显示方向序列
        if (pathData.directionSequence.Count > 0)
        {
            pathSlot.nameText.text = pathData.GetDirectionSequenceString();
        }
        else
        {
            pathSlot.nameText.text = pathData.pathName;
        }
    }
    
    private void OnDirectionSequenceChanged(List<Direction> directionSequence)
    {
        // 更新方向序列文本
        if (directionSequenceText != null)
        {
            string sequenceStr = "";
            foreach (Direction dir in directionSequence)
            {
                switch (dir)
                {
                    case Direction.Up:
                        sequenceStr += "↑";
                        break;
                    case Direction.Right:
                        sequenceStr += "→";
                        break;
                    case Direction.Down:
                        sequenceStr += "↓";
                        break;
                    case Direction.Left:
                        sequenceStr += "←";
                        break;
                }
            }
            directionSequenceText.text = sequenceStr;
        }
    }
    
    private void OnMatchedPathsChanged(List<PathDataSO> matchedPaths)
    {
        // 更新所有槽位的显示状态
        foreach (PathSlot slot in pathSlots)
        {
            if (slot == null) continue;
            
            bool isMatched = matchedPaths.Contains(slot.GetPathData());
            slot.SetHighlighted(isMatched);
        }
    }
    
    private void OnPreviewPathChanged(PathDataSO pathData)
    {
        // 更新预览路径的显示状态
        foreach (PathSlot slot in pathSlots)
        {
            if (slot == null) continue;
            
            bool isPreview = slot.GetPathData() == pathData;
            slot.SetPreview(isPreview);
        }
    }
    
    private void OnInputReset()
    {
        // 重置所有槽位的显示状态
        foreach (PathSlot slot in pathSlots)
        {
            if (slot == null) continue;
            
            slot.SetHighlighted(false);
            slot.SetPreview(false);
        }
        
        // 清空方向序列文本
        if (directionSequenceText != null)
        {
            directionSequenceText.text = "";
        }
    }
} 