using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class ButtonHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Text text;
    public int normalFontSize = 36;
    public int highlightedFontSize = 44;
    public FontStyle normalFontStyle = FontStyle.Normal;
    public FontStyle highlightedFontStyle = FontStyle.Bold;
    public Color normalColor = Color.gray;
    public Color highlightedColor = Color.white;
    public float transitionSpeed = 10f; // 过渡速度

    private int targetFontSize;
    private Color targetColor;
    private Coroutine animCoroutine;

    void Reset()
    {
        text = GetComponentInChildren<Text>();
    }

    void Start()
    {
        if (text != null)
        {
            targetFontSize = normalFontSize;
            targetColor = normalColor;
            text.fontSize = normalFontSize;
            text.fontStyle = normalFontStyle;
            text.color = normalColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetFontSize = highlightedFontSize;
        targetColor = highlightedColor;
        text.fontStyle = highlightedFontStyle;
        
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(AnimateTransition());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetFontSize = normalFontSize;
        targetColor = normalColor;
        text.fontStyle = normalFontStyle;
        
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(AnimateTransition());
    }

    IEnumerator AnimateTransition()
    {
        while (Mathf.Abs(text.fontSize - targetFontSize) > 0.1f || 
               Vector4.Distance(text.color, targetColor) > 0.01f)
        {
            // 字体大小过渡
            text.fontSize = Mathf.RoundToInt(Mathf.Lerp(text.fontSize, targetFontSize, Time.deltaTime * transitionSpeed));
            
            // 颜色过渡
            text.color = Color.Lerp(text.color, targetColor, Time.deltaTime * transitionSpeed);
            
            yield return null;
        }
        
        text.fontSize = targetFontSize;
        text.color = targetColor;
    }
}