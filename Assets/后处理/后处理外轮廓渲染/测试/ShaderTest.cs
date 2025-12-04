using UnityEngine;

public class ShaderTest : MonoBehaviour
{
    public Shader drawOccupiedShader;
    public Shader outlineShader;
    public Shader blendShader;

    void Start()
    {
        TestShader("DrawOccupied", drawOccupiedShader);
        TestShader("Outline", outlineShader);
        TestShader("Blend", blendShader);
    }

    void TestShader(string name, Shader shader)
    {
        if (shader == null)
        {
            Debug.LogError($"{name} Shader 为空！");
            return;
        }

        Debug.Log($"{name} Shader: {shader.name}");
        Debug.Log($"{name} 是否支持: {shader.isSupported}");

        if (!shader.isSupported)
        {
            Debug.LogError($"{name} Shader 不支持当前平台！");
        }
    }
}