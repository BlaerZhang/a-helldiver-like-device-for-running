using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class PathSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    public RawImage iconImage;
    public TextMeshProUGUI nameText;
    public Image background;
    
    private Button button;
    private PathDataSO pathData;
    private bool isHighlighted = false;
    private bool isPreview = false;
    private bool isHovered = false;
    
    private void Awake()
    {
        // 获取必要组件
        button = GetComponent<Button>();
        background = GetComponent<Image>();
        
        // 确保图标和文本的布局正确
        SetupLayout();
    }
    
    private void SetupLayout()
    {
        if (iconImage != null)
        {
            // 设置图标的RectTransform
            RectTransform iconRect = iconImage.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.2f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = Vector2.zero;
            
            // 添加AspectRatioFitter确保图标保持正方形
            AspectRatioFitter fitter = iconImage.GetComponent<AspectRatioFitter>();
            if (fitter == null)
            {
                fitter = iconImage.gameObject.AddComponent<AspectRatioFitter>();
            }
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;
        }
        
        if (nameText != null)
        {
            // 设置文本的RectTransform
            RectTransform textRect = nameText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 0.2f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;
            
            // 配置文本组件
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 8;
            nameText.fontSizeMax = 75;
        }
    }
    
    public void SetPathData(PathDataSO data)
    {
        if (data == null)
        {
            Debug.LogError("PathSlot: Trying to set null path data!");
            return;
        }
        
        pathData = data;
        
        if (nameText != null)
        {
            nameText.text = data.pathName;
        }
    }
    
    public PathDataSO GetPathData()
    {
        return pathData;
    }
    
    public void SetHighlighted(bool highlighted)
    {
        isHighlighted = highlighted;
        UpdateVisualState();
    }
    
    public void SetPreview(bool preview)
    {
        isPreview = preview;
        UpdateVisualState();
    }
    
    private void UpdateVisualState()
    {
        if (background == null) return;
        
        if (!isHighlighted)
        {
            // 未匹配的路径显示为禁用状态
            background.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            if (iconImage != null)
            {
                iconImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }
        else if (isPreview)
        {
            // 预览中的路径显示为预览状态
            background.color = new Color(0.8f, 0.8f, 0.4f, 0.8f);
            if (iconImage != null)
            {
                iconImage.color = Color.white;
            }
        }
        else if (isHovered)
        {
            // 鼠标悬停的路径显示为高亮状态
            background.color = new Color(0.4f, 0.8f, 0.4f, 0.8f);
            if (iconImage != null)
            {
                iconImage.color = Color.white;
            }
        }
        else
        {
            // 匹配但未悬停的路径显示为正常状态
            background.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            if (iconImage != null)
            {
                iconImage.color = Color.white;
            }
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        // isHovered = true;
        // UpdateVisualState();
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        // isHovered = false;
        // UpdateVisualState();
    }
    
    private void OnDestroy()
    {
        // 清理事件监听
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }
    }
} 