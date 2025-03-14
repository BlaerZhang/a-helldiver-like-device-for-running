using UnityEngine;
using System.Collections.Generic;

namespace FogOfWar
{
    public class FogOfWarManager : MonoBehaviour
    {
        [Header("迷雾设置")]
        [SerializeField] private Transform playerTransform; // 玩家的Transform
        [SerializeField] private float visibilityRadius = 5f; // 玩家可见半径
        [SerializeField] private float blurSize = 1f; // 边缘模糊大小
        [SerializeField] private int textureSize = 1024; // 纹理大小
        [SerializeField] private Color fogColor = new Color(0, 0, 0, 0.9f); // 迷雾颜色
        
        [Header("地图设置")]
        [SerializeField] private float mapWidth = 100f; // 地图宽度
        [SerializeField] private float mapHeight = 100f; // 地图高度
        [SerializeField] private float fogScale = 9f; // 迷雾缩放系数，直接控制迷雾对象的缩放
        [SerializeField] private bool autoAdjustScale = true; // 是否自动调整缩放
        [SerializeField] private bool adjustToMapSize = true; // 是否根据地图大小调整迷雾尺寸
        
        [Header("调试")]
        [SerializeField] private bool showDebugGizmos = false; // 是否显示调试图形
        
        private Texture2D fogTexture; // 迷雾纹理
        private SpriteRenderer fogRenderer; // 迷雾渲染器
        private GameObject fogObject; // 迷雾游戏对象
        private Color[] clearColors; // 清除的颜色数组
        private List<Vector2> exploredPositions = new List<Vector2>(); // 已探索的位置
        private Vector2 fogOrigin; // 迷雾原点（世界坐标）
        
        // 单例模式
        public static FogOfWarManager Instance { get; private set; }
        
        private void Awake()
        {
            // 单例设置
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // 初始化迷雾
            InitializeFog();
        }
        
        private void Start()
        {
            // 如果没有指定玩家，尝试查找
            if (playerTransform == null)
            {
                PlayerController player = FindObjectOfType<PlayerController>();
                if (player != null)
                {
                    playerTransform = player.transform;
                }
            }
            
            // 如果启用自动调整，尝试检测地图边界
            if (autoAdjustScale)
            {
                DetectMapBounds();
            }
            
            // 计算迷雾原点
            CalculateFogOrigin();
        }
        
        private void OnDestroy()
        {
            // 不再需要取消订阅事件
        }
        
        private void Update()
        {
            // 如果玩家存在，更新玩家当前位置的可见性
            if (playerTransform != null)
            {
                RevealArea(playerTransform.position);
            }
            
            // 实时更新迷雾缩放（仅在编辑器中）
            #if UNITY_EDITOR
            if (fogObject != null && Application.isEditor)
            {
                UpdateFogScale();
                CalculateFogOrigin();
            }
            #endif
        }
        
        private void InitializeFog()
        {
            // 创建迷雾纹理
            fogTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            fogTexture.filterMode = FilterMode.Bilinear; // 使用双线性过滤使边缘更平滑
            
            // 初始化为完全不透明的黑色
            Color[] colors = new Color[textureSize * textureSize];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = fogColor;
            }
            fogTexture.SetPixels(colors);
            fogTexture.Apply();
            
            // 创建迷雾精灵
            fogObject = new GameObject("FogOfWar");
            fogObject.transform.parent = transform;
            fogRenderer = fogObject.AddComponent<SpriteRenderer>();
            
            // 创建精灵
            Sprite fogSprite = Sprite.Create(
                fogTexture, 
                new Rect(0, 0, textureSize, textureSize), 
                new Vector2(0.5f, 0.5f), 
                100f
            );
            
            // 设置精灵
            fogRenderer.sprite = fogSprite;
            fogRenderer.sortingOrder = -1; // 确保在大多数对象之上
            
            // 设置迷雾对象的位置和缩放
            fogObject.transform.position = new Vector3(0, 0, -1); // 略微在前景之后
            UpdateFogScale();
            
            // 计算迷雾原点
            CalculateFogOrigin();
        }
        
        // 计算迷雾原点（世界坐标）
        private void CalculateFogOrigin()
        {
            if (fogObject != null)
            {
                // 迷雾精灵的中心点对应纹理的中心点
                fogOrigin = fogObject.transform.position;
                
                // 考虑精灵的缩放
                float textureWorldSize = GetTextureWorldSize();
                
                // 迷雾原点是左下角
                fogOrigin.x -= textureWorldSize / 2;
                fogOrigin.y -= textureWorldSize / 2;
                
                if (showDebugGizmos)
                {
                    Debug.Log($"迷雾原点: {fogOrigin}, 纹理世界大小: {textureWorldSize}");
                }
            }
        }
        
        // 获取纹理的世界大小
        private float GetTextureWorldSize()
        {
            if (adjustToMapSize)
            {
                // 使用地图尺寸来确定迷雾大小
                return Mathf.Max(mapWidth, mapHeight);
            }
            else
            {
                // 使用固定的缩放系数
                return fogScale * 10f; // 10是基准值，与精灵创建时的pixelsPerUnit=100对应
            }
        }
        
        // 更新迷雾缩放
        private void UpdateFogScale()
        {
            if (fogObject != null)
            {
                if (adjustToMapSize)
                {
                    // 根据地图大小调整缩放
                    float textureWorldSize = GetTextureWorldSize();
                    float newScale = textureWorldSize / 10f; // 10是基准值，与精灵创建时的pixelsPerUnit=100对应
                    fogObject.transform.localScale = new Vector3(newScale, newScale, 1);
                    fogScale = newScale; // 更新fogScale以保持一致
                }
                else
                {
                    // 使用固定的缩放系数
                    fogObject.transform.localScale = new Vector3(fogScale, fogScale, 1);
                }
            }
        }
        
        // 尝试检测地图边界并自动调整迷雾缩放
        private void DetectMapBounds()
        {
            // 查找所有地形/背景对象
            Renderer[] renderers = FindObjectsOfType<Renderer>();
            if (renderers.Length == 0) return;
            
            // 计算所有渲染器的边界
            Bounds mapBounds = new Bounds();
            bool boundsInitialized = false;
            
            foreach (Renderer renderer in renderers)
            {
                // 排除迷雾自身
                if (renderer == fogRenderer) continue;
                
                // 排除UI元素和其他不应该计入地图大小的对象
                if (renderer.gameObject.layer == LayerMask.NameToLayer("UI")) continue;
                
                if (!boundsInitialized)
                {
                    mapBounds = renderer.bounds;
                    boundsInitialized = true;
                }
                else
                {
                    mapBounds.Encapsulate(renderer.bounds);
                }
            }
            
            if (boundsInitialized)
            {
                // 更新地图尺寸
                mapWidth = mapBounds.size.x;
                mapHeight = mapBounds.size.y;
                
                // 计算合适的缩放值
                float maxDimension = Mathf.Max(mapWidth, mapHeight);
                
                if (adjustToMapSize)
                {
                    // 直接使用地图尺寸
                    Debug.Log($"自动检测到地图边界: 宽={mapWidth}, 高={mapHeight}, 迷雾将覆盖整个地图");
                }
                else
                {
                    // 使用缩放系数
                    fogScale = maxDimension / 10f; // 10是基准值，可以根据需要调整
                    Debug.Log($"自动检测到地图边界: 宽={mapWidth}, 高={mapHeight}, 设置迷雾缩放为: {fogScale}");
                }
                
                // 更新迷雾缩放
                UpdateFogScale();
                
                // 重新计算迷雾原点
                CalculateFogOrigin();
            }
        }
        
        public void RevealArea(Vector2 worldPosition)
        {
            // 计算世界坐标相对于迷雾原点的位置
            Vector2 relativePos = worldPosition - fogOrigin;
            
            // 计算纹理上的位置（0-1范围）
            float textureWorldSize = GetTextureWorldSize();
            Vector2 normalizedPos = new Vector2(
                relativePos.x / textureWorldSize,
                relativePos.y / textureWorldSize
            );
            
            // 确保坐标在有效范围内
            if (normalizedPos.x < 0 || normalizedPos.x > 1 || normalizedPos.y < 0 || normalizedPos.y > 1)
            {
                if (showDebugGizmos)
                {
                    Debug.LogWarning($"位置 {worldPosition} 超出迷雾范围! 相对位置: {relativePos}, 归一化位置: {normalizedPos}");
                }
                return;
            }
            
            // 转换为像素坐标
            int centerX = Mathf.RoundToInt(normalizedPos.x * textureSize);
            int centerY = Mathf.RoundToInt(normalizedPos.y * textureSize);
            
            // 计算半径（像素）
            int pixelRadius = Mathf.RoundToInt((visibilityRadius / textureWorldSize) * textureSize);
            
            // 添加到已探索位置
            exploredPositions.Add(worldPosition);
            
            // 更新纹理
            UpdateFogTexture(centerX, centerY, pixelRadius);
            
            if (showDebugGizmos)
            {
                Debug.DrawLine(worldPosition, fogOrigin, Color.red, 0.1f);
                Debug.Log($"显示区域: 世界位置={worldPosition}, 纹理位置=({centerX},{centerY}), 半径={pixelRadius}像素");
            }
        }
        
        private void UpdateFogTexture(int centerX, int centerY, int radius)
        {
            // 获取当前像素
            Color[] pixels = fogTexture.GetPixels();
            
            // 计算边界
            int startX = Mathf.Max(0, centerX - radius);
            int endX = Mathf.Min(textureSize - 1, centerX + radius);
            int startY = Mathf.Max(0, centerY - radius);
            int endY = Mathf.Min(textureSize - 1, centerY + radius);
            
            // 更新像素
            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    // 计算到中心的距离
                    float distance = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                    
                    // 如果在半径内，清除迷雾
                    if (distance <= radius)
                    {
                        int index = y * textureSize + x;
                        
                        // 计算透明度 - 边缘模糊效果
                        float alpha = distance > radius - blurSize 
                            ? (radius - distance) / blurSize * fogColor.a 
                            : 0;
                        
                        // 只有当新的透明度更低时才更新
                        if (alpha < pixels[index].a)
                        {
                            pixels[index].a = alpha;
                        }
                    }
                }
            }
            
            // 应用更改
            fogTexture.SetPixels(pixels);
            fogTexture.Apply();
        }
        
        // 重置迷雾（用于测试或关卡重置）
        public void ResetFog()
        {
            // 重置为完全不透明
            Color[] colors = new Color[textureSize * textureSize];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = fogColor;
            }
            fogTexture.SetPixels(colors);
            fogTexture.Apply();
            
            // 清除已探索位置
            exploredPositions.Clear();
        }
        
        // 手动设置迷雾缩放
        public void SetFogScale(float scale)
        {
            fogScale = scale;
            UpdateFogScale();
            CalculateFogOrigin();
        }
        
        // 获取当前迷雾缩放
        public float GetFogScale()
        {
            return fogScale;
        }
        
        // 绘制调试图形
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || !Application.isPlaying) return;
            
            // 绘制迷雾原点
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(fogOrigin, 0.5f);
            
            // 绘制迷雾边界
            Gizmos.color = Color.yellow;
            float textureWorldSize = GetTextureWorldSize();
            Vector3 topRight = new Vector3(fogOrigin.x + textureWorldSize, fogOrigin.y + textureWorldSize, 0);
            Gizmos.DrawLine(new Vector3(fogOrigin.x, fogOrigin.y, 0), new Vector3(topRight.x, fogOrigin.y, 0));
            Gizmos.DrawLine(new Vector3(topRight.x, fogOrigin.y, 0), new Vector3(topRight.x, topRight.y, 0));
            Gizmos.DrawLine(new Vector3(topRight.x, topRight.y, 0), new Vector3(fogOrigin.x, topRight.y, 0));
            Gizmos.DrawLine(new Vector3(fogOrigin.x, topRight.y, 0), new Vector3(fogOrigin.x, fogOrigin.y, 0));
            
            // 绘制已探索位置
            Gizmos.color = Color.green;
            foreach (Vector2 pos in exploredPositions)
            {
                Gizmos.DrawSphere(pos, 0.2f);
            }
            
            // 绘制玩家可见半径
            if (playerTransform != null)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawSphere(playerTransform.position, visibilityRadius);
            }
        }
    }
} 