#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(OutlineDisplayManager))]
public class OutlineDisplayManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        OutlineDisplayManager manager = (OutlineDisplayManager)target;

        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox(manager.GetStats(), MessageType.Info);

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("刷新Layer映射"))
            {
                manager.RefreshLayerMappings();
            }
            
            if (GUILayout.Button("清理全部"))
            {
                manager.CleanupAll();
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif