using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DirectionSequenceDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI sequenceText;
    public RectTransform arrowContainer;
    public GameObject arrowPrefab;
    
    [Header("Visual Settings")]
    public float arrowSpacing = 10f;
    public Vector2 arrowSize = new Vector2(30, 30);
    public Color normalColor = Color.white;
    public Color matchedColor = Color.green;
    
    private List<Image> arrowImages = new List<Image>();
    
    private void Start()
    {
        // 订阅方向输入管理器的事件
        if (DirectionInputManager.Instance != null)
        {
            DirectionInputManager.Instance.OnDirectionSequenceChanged += OnDirectionSequenceChanged;
            DirectionInputManager.Instance.OnInputReset += OnInputReset;
        }
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        if (DirectionInputManager.Instance != null)
        {
            DirectionInputManager.Instance.OnDirectionSequenceChanged -= OnDirectionSequenceChanged;
            DirectionInputManager.Instance.OnInputReset -= OnInputReset;
        }
    }
    
    private void OnDirectionSequenceChanged(List<Direction> directionSequence)
    {
        // 更新文本显示
        if (sequenceText != null)
        {
            string sequenceStr = GetDirectionSequenceString(directionSequence);
            sequenceText.text = sequenceStr;
        }
        
        // 更新箭头显示
        UpdateArrowDisplay(directionSequence);
    }
    
    private void OnInputReset()
    {
        // 清空文本
        if (sequenceText != null)
        {
            sequenceText.text = "";
        }
        
        // 清空箭头
        ClearArrows();
    }
    
    private void UpdateArrowDisplay(List<Direction> directionSequence)
    {
        // 清空现有箭头
        ClearArrows();
        
        if (arrowContainer == null || arrowPrefab == null) return;
        
        // 创建新箭头
        for (int i = 0; i < directionSequence.Count; i++)
        {
            GameObject arrowObj = Instantiate(arrowPrefab, arrowContainer);
            Image arrowImage = arrowObj.GetComponent<Image>();
            arrowImages.Add(arrowImage);
            
            // 设置箭头位置
            RectTransform arrowRect = arrowObj.GetComponent<RectTransform>();
            arrowRect.sizeDelta = arrowSize;
            arrowRect.anchoredPosition = new Vector2(i * (arrowSize.x + arrowSpacing), 0);
            
            // 设置箭头旋转
            float rotation = 0;
            switch (directionSequence[i])
            {
                case Direction.Up:
                    rotation = 0;
                    break;
                case Direction.Right:
                    rotation = 90;
                    break;
                case Direction.Down:
                    rotation = 180;
                    break;
                case Direction.Left:
                    rotation = 270;
                    break;
            }
            arrowRect.rotation = Quaternion.Euler(0, 0, -rotation);
            
            // 设置箭头颜色
            arrowImage.color = normalColor;
        }
        
        // 调整容器大小
        if (directionSequence.Count > 0)
        {
            float totalWidth = directionSequence.Count * arrowSize.x + (directionSequence.Count - 1) * arrowSpacing;
            arrowContainer.sizeDelta = new Vector2(totalWidth, arrowSize.y);
        }
        else
        {
            arrowContainer.sizeDelta = Vector2.zero;
        }
    }
    
    private void ClearArrows()
    {
        foreach (Image arrow in arrowImages)
        {
            if (arrow != null)
            {
                Destroy(arrow.gameObject);
            }
        }
        arrowImages.Clear();
    }
    
    private string GetDirectionSequenceString(List<Direction> directionSequence)
    {
        string result = "";
        foreach (Direction dir in directionSequence)
        {
            switch (dir)
            {
                case Direction.Up:
                    result += "↑";
                    break;
                case Direction.Right:
                    result += "→";
                    break;
                case Direction.Down:
                    result += "↓";
                    break;
                case Direction.Left:
                    result += "←";
                    break;
            }
        }
        return result;
    }
} 