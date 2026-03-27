using UnityEngine;
using UnityEngine.UI;
using TMPro; // 如果你用的是 TextMeshPro

[ExecuteAlways] // 让你在编辑模式下也能看到效果
[RequireComponent(typeof(LayoutElement))]
public class ContentSizeFitterWithMax : MonoBehaviour
{
    public float maxWidth = 500f; // 你想要的最大宽度
    public Text textComponent; // 你的文本组件
    private LayoutElement layoutElement;

    void OnEnable()
    {
        layoutElement = GetComponent<LayoutElement>();
        if (textComponent == null) textComponent = GetComponent<Text>();
    }

    void Update()
    {
        if (textComponent != null && layoutElement != null)
        {
            // 核心逻辑：首选宽度 = Min(文字内容的自然宽度, 最大限制宽度)
            // 这样文字少时，宽度就是文字宽；文字多时，宽度被卡在 maxWidth
            layoutElement.preferredWidth = Mathf.Min(textComponent.preferredWidth, maxWidth);
        }
    }
}