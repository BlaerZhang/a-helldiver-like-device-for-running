using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public static class PathIconGenerator
{
    private const float PADDING = 0.3f; // 增加边距，让路径显示区域更集中
    private const float LINE_WIDTH = 16f; // 增加线条宽度
    private const float START_POINT_RADIUS = 8f; // 起点圆圈的半径
    private const float ARROW_SIZE = 14f; // 增大箭头大小，使其更醒目
    
    public static void GeneratePathIcon(PathDataSO pathData, RawImage targetImage)
    {
        if (pathData == null || targetImage == null) return;
        
        // 生成路径点
        pathData.GeneratePath();
        List<Vector2> pathPoints = pathData.GetPathPoints();
        
        if (pathPoints == null || pathPoints.Count < 2)
        {
            Debug.LogWarning($"Invalid path points for {pathData.name}");
            return;
        }
        
        // 创建纹理 - 增加纹理大小以提高清晰度
        int textureSize = 256;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        
        // 填充透明背景
        Color[] pixels = new Color[textureSize * textureSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(0, 0, 0, 0);
        }
        texture.SetPixels(pixels);
        
        // 计算路径的边界
        Vector2 min = pathPoints[0];
        Vector2 max = pathPoints[0];
        foreach (Vector2 point in pathPoints)
        {
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        
        // 确保路径有最小尺寸，防止单点路径
        if ((max - min).magnitude < 0.1f)
        {
            min -= new Vector2(1f, 1f);
            max += new Vector2(1f, 1f);
        }
        
        // 计算缩放因子，使路径适应纹理大小
        Vector2 size = max - min;
        float scale = Mathf.Min(
            (textureSize * (1 - PADDING * 2)) / Mathf.Max(1f, size.x),
            (textureSize * (1 - PADDING * 2)) / Mathf.Max(1f, size.y)
        );
        
        // 计算偏移，使路径居中
        Vector2 center = (min + max) * 0.5f;
        Vector2 offset = new Vector2(textureSize * 0.5f, textureSize * 0.5f);
        
        // 绘制路径
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Vector2 start = (pathPoints[i] - center) * scale + offset;
            Vector2 end = (pathPoints[i + 1] - center) * scale + offset;
            DrawLine(texture, start, end, new Color(1f, 1f, 1f, 1f), LINE_WIDTH); // 使用完全不透明的白色
        }
        
        // 绘制起点标记（绿色圆圈）
        Vector2 startPoint = (pathPoints[0] - center) * scale + offset;
        DrawStartPoint(texture, startPoint, new Color(0f, 1f, 0f, 1f), START_POINT_RADIUS); // 绿色起点
        
        // 绘制终点箭头（红色）
        Vector2 lastPoint = (pathPoints[pathPoints.Count - 1] - center) * scale + offset;
        Vector2 secondLastPoint = (pathPoints[pathPoints.Count - 2] - center) * scale + offset;
        Vector2 direction = (lastPoint - secondLastPoint).normalized;
        DrawArrow(texture, lastPoint, direction, new Color(1f, 0f, 0f, 1f), ARROW_SIZE); // 红色箭头
        
        // 应用纹理
        texture.Apply();
        
        // 设置纹理属性
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        
        // 应用到RawImage
        targetImage.texture = texture;
        
        // 确保RawImage的AspectRatioFitter设置正确
        if (targetImage.GetComponent<UnityEngine.UI.AspectRatioFitter>() == null)
        {
            UnityEngine.UI.AspectRatioFitter fitter = targetImage.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            fitter.aspectRatio = 1f;
            fitter.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
        }
    }
    
    private static void DrawLine(Texture2D tex, Vector2 start, Vector2 end, Color color, float width)
    {
        Vector2 dir = (end - start).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x) * (width * 0.5f);
        
        float distance = Vector2.Distance(start, end);
        int steps = Mathf.CeilToInt(distance * 2); // 增加采样点，使线条更平滑
        
        for (float i = 0; i <= distance; i += 0.5f) // 减小步长，使线条更平滑
        {
            float t = i / distance;
            Vector2 pos = Vector2.Lerp(start, end, t);
            
            // 绘制线条宽度
            for (float w = -width/2; w <= width/2; w += 0.5f) // 减小步长，使线条更平滑
            {
                Vector2 pixelPos = pos + normal * (w / width);
                int x = Mathf.RoundToInt(pixelPos.x);
                int y = Mathf.RoundToInt(pixelPos.y);
                
                if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                {
                    // 使用抗锯齿
                    float alpha = 1f - (Mathf.Abs(w) / (width * 0.5f)) * 0.5f; // 减少alpha衰减，使线条更明显
                    Color pixelColor = color * alpha;
                    tex.SetPixel(x, y, Color.Lerp(tex.GetPixel(x, y), pixelColor, alpha));
                }
            }
        }
    }
    
    private static void DrawStartPoint(Texture2D tex, Vector2 center, Color color, float radius)
    {
        // 绘制圆形起点标记
        for (float x = -radius; x <= radius; x += 0.5f)
        {
            for (float y = -radius; y <= radius; y += 0.5f)
            {
                float distance = Mathf.Sqrt(x * x + y * y);
                if (distance <= radius)
                {
                    int pixelX = Mathf.RoundToInt(center.x + x);
                    int pixelY = Mathf.RoundToInt(center.y + y);
                    
                    if (pixelX >= 0 && pixelX < tex.width && pixelY >= 0 && pixelY < tex.height)
                    {
                        // 使用抗锯齿
                        float alpha = 1f - (distance / radius) * 0.5f;
                        Color pixelColor = color * alpha;
                        tex.SetPixel(pixelX, pixelY, Color.Lerp(tex.GetPixel(pixelX, pixelY), pixelColor, alpha));
                    }
                }
            }
        }
    }
    
    private static void DrawArrow(Texture2D tex, Vector2 position, Vector2 direction, Color color, float size)
    {
        // 计算箭头的三个点
        Vector2 right = new Vector2(-direction.y, direction.x); // 垂直于方向的向量
        Vector2 tip = position + direction * size;
        Vector2 baseLeft = position - right * (size * 0.5f);
        Vector2 baseRight = position + right * (size * 0.5f);
        
        // 绘制箭头的三条线
        DrawLine(tex, baseLeft, tip, color, LINE_WIDTH * 0.5f);
        DrawLine(tex, baseRight, tip, color, LINE_WIDTH * 0.5f);
        DrawLine(tex, baseLeft, baseRight, color, LINE_WIDTH * 0.5f);
    }
} 