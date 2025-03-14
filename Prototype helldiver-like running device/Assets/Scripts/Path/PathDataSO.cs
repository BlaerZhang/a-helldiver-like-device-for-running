using UnityEngine;
using System.Collections.Generic;

public enum PathType
{
    Linear = 0,
    Bezier = 1,
    CatmullRom = 2
}

[CreateAssetMenu(fileName = "New Path", menuName = "Game/Path Data")]
public class PathDataSO : ScriptableObject
{
    [Header("基本信息")]
    public string pathName;
    public string description;
    
    [Header("路径类型")]
    public PathType pathType = PathType.Linear;
    
    [Header("方向序列")]
    [Tooltip("路径的方向键序列，用于玩家通过方向键选择")]
    public List<Direction> directionSequence = new List<Direction>();
    
    [Header("路径点")]
    public List<Vector2> controlPoints = new List<Vector2>();
    
    [Header("贝塞尔曲线控制点")]
    public List<Vector2> control1 = new List<Vector2>();
    public List<Vector2> control2 = new List<Vector2>();
    
    [Header("生成设置")]
    public int resolution = 10; // 曲线分辨率
    
    // 中间点（生成的路径点）
    private List<Vector2> intermediatePoints = new List<Vector2>();
    
    // 生成路径
    public void GeneratePath()
    {
        intermediatePoints.Clear();
        
        if (controlPoints.Count < 2)
        {
            Debug.LogWarning($"Path {name} has less than 2 control points!");
            return;
        }
        
        switch (pathType)
        {
            case PathType.Linear:
                GenerateLinearPath();
                break;
            case PathType.Bezier:
                GenerateBezierPath();
                break;
            case PathType.CatmullRom:
                GenerateCatmullRomPath();
                break;
        }
    }
    
    // 获取路径点
    public List<Vector2> GetPathPoints()
    {
        if (intermediatePoints.Count == 0)
        {
            GeneratePath();
        }
        return intermediatePoints;
    }
    
    // 生成线性路径
    private void GenerateLinearPath()
    {
        for (int i = 0; i < controlPoints.Count - 1; i++)
        {
            Vector2 start = controlPoints[i];
            Vector2 end = controlPoints[i + 1];
            
            for (int j = 0; j <= resolution; j++)
            {
                float t = j / (float)resolution;
                Vector2 point = Vector2.Lerp(start, end, t);
                intermediatePoints.Add(point);
            }
        }
    }
    
    // 生成贝塞尔曲线路径
    private void GenerateBezierPath()
    {
        for (int i = 0; i < controlPoints.Count - 1; i++)
        {
            Vector2 p0 = controlPoints[i];
            Vector2 p3 = controlPoints[i + 1];
            
            // 确保控制点数组长度足够
            if (i >= control1.Count || i >= control2.Count)
            {
                Debug.LogWarning($"Path {name} is missing control points for segment {i}!");
                continue;
            }
            
            Vector2 p1 = control1[i];
            Vector2 p2 = control2[i];
            
            for (int j = 0; j <= resolution; j++)
            {
                float t = j / (float)resolution;
                Vector2 point = CalculateBezierPoint(p0, p1, p2, p3, t);
                intermediatePoints.Add(point);
            }
        }
    }
    
    // 计算贝塞尔曲线上的点
    private Vector2 CalculateBezierPoint(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;
        
        Vector2 p = uuu * p0;
        p += 3 * uu * t * p1;
        p += 3 * u * tt * p2;
        p += ttt * p3;
        
        return p;
    }
    
    // 生成Catmull-Rom样条曲线路径
    private void GenerateCatmullRomPath()
    {
        if (controlPoints.Count < 4)
        {
            Debug.LogWarning($"Path {name} has less than 4 control points for Catmull-Rom!");
            return;
        }
        
        for (int i = 0; i < controlPoints.Count - 3; i++)
        {
            Vector2 p0 = controlPoints[i];
            Vector2 p1 = controlPoints[i + 1];
            Vector2 p2 = controlPoints[i + 2];
            Vector2 p3 = controlPoints[i + 3];
            
            for (int j = 0; j <= resolution; j++)
            {
                float t = j / (float)resolution;
                Vector2 point = CalculateCatmullRomPoint(p0, p1, p2, p3, t);
                intermediatePoints.Add(point);
            }
        }
    }
    
    // 计算Catmull-Rom样条曲线上的点
    private Vector2 CalculateCatmullRomPoint(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        
        Vector2 a = 0.5f * (2f * p1);
        Vector2 b = 0.5f * (p2 - p0);
        Vector2 c = 0.5f * (2f * p0 - 5f * p1 + 4f * p2 - p3);
        Vector2 d = 0.5f * (-p0 + 3f * p1 - 3f * p2 + p3);
        
        return a + (b * t) + (c * t2) + (d * t3);
    }
    
    // 获取方向序列的字符串表示
    public string GetDirectionSequenceString()
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