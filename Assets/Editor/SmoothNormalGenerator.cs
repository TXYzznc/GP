using UnityEngine;
using UnityEditor;

public class SmoothNormalGenerator : Editor
{
    [MenuItem("Tools/将平滑法线写入Tangent")]
    static void BakeSmoothNormal()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                Mesh mesh = meshFilter.sharedMesh;
                BakeSmoothNormalToTangent(mesh);
                Debug.Log($"✅ 已处理: {obj.name}");
            }
        }
    }

    static void BakeSmoothNormalToTangent(Mesh mesh)
    {
        // 1. 计算平滑法线
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector4[] tangents = new Vector4[vertices.Length];

        // 使用字典存储位置相同顶点的平均法线
        var normalDict = new System.Collections.Generic.Dictionary<Vector3, Vector3>();
        
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 pos = vertices[i];
            if (!normalDict.ContainsKey(pos))
                normalDict[pos] = Vector3.zero;
            
            normalDict[pos] += normals[i];
        }

        // 归一化并存储到tangent
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 smoothNormal = normalDict[vertices[i]].normalized;
            tangents[i] = new Vector4(smoothNormal.x, smoothNormal.y, smoothNormal.z, 0);
        }

        mesh.tangents = tangents;
    }
}