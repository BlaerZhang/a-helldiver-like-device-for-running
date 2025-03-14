#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace FogOfWar
{
    public static class FogOfWarPrefabCreator
    {
        [MenuItem("GameObject/2D Object/Fog of War")]
        public static void CreateFogOfWarPrefab()
        {
            // 创建战争迷雾管理器对象
            GameObject fogOfWarObj = new GameObject("FogOfWarManager");
            FogOfWarManager fogOfWarManager = fogOfWarObj.AddComponent<FogOfWarManager>();
            
            // 设置一些默认值
            SerializedObject serializedObject = new SerializedObject(fogOfWarManager);
            
            // 设置默认的迷雾缩放为9（根据用户反馈）
            SerializedProperty fogScaleProp = serializedObject.FindProperty("fogScale");
            if (fogScaleProp != null)
            {
                fogScaleProp.floatValue = 9f;
            }
            
            // 设置默认的可见半径
            SerializedProperty visibilityRadiusProp = serializedObject.FindProperty("visibilityRadius");
            if (visibilityRadiusProp != null)
            {
                visibilityRadiusProp.floatValue = 5f;
            }
            
            // 应用更改
            serializedObject.ApplyModifiedProperties();
            
            // 选中新创建的对象
            Selection.activeGameObject = fogOfWarObj;
            
            // 将对象放置在场景视图中心
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                fogOfWarObj.transform.position = new Vector3(0, 0, 0);
            }
            
            Undo.RegisterCreatedObjectUndo(fogOfWarObj, "Create Fog of War Manager");
            
            Debug.Log("战争迷雾管理器已创建。默认缩放设置为9，您可以在Inspector中进一步调整。");
        }
    }
}
#endif 