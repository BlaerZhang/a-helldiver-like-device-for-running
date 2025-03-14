using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlayerPathInventory))]
public class PlayerPathInventoryEditor : Editor
{
    private PathDataSO newPath;
    
    public override void OnInspectorGUI()
    {
        PlayerPathInventory inventory = (PlayerPathInventory)target;
        
        // 绘制默认的Inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("快速添加/移除路径", EditorStyles.boldLabel);
        
        // 创建一个水平布局组
        EditorGUILayout.BeginHorizontal();
        
        // 创建一个对象选择字段
        newPath = (PathDataSO)EditorGUILayout.ObjectField("新路径", newPath, typeof(PathDataSO), false);
        
        // 添加按钮
        if (GUILayout.Button("添加路径", GUILayout.Width(80)))
        {
            if (newPath != null)
            {
                inventory.EditorAddPath(newPath);
                newPath = null; // 清空选择
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 如果在编辑模式下修改了组件，标记为已修改
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
} 