using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityGameFramework.Runtime;

/// <summary>
/// 打字机效果组件
/// 支持 Text 和 TextMesh 组件
/// 可配合多语言系统使用
/// </summary>
public class TypewriterEffect : MonoBehaviour
{
    #region 配置参数

    [Header("多语言配置")]
    [Tooltip("多语言Key（如果为空则使用Text组件的初始文本）")]
    [SerializeField] private string localizationKey = "";

    [Header("打字机效果配置")]
    [Tooltip("每个字符的显示间隔时间（秒）")]
    [SerializeField] private float typeSpeed = 0.05f;

    [Tooltip("是否在Start时自动播放")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("打字完成后的等待时间（秒）")]
    [SerializeField] private float waitTimeAfterComplete = 1f;

    [Tooltip("是否在完成后自动淡出")]
    [SerializeField] private bool fadeOutAfterComplete = false;

    [Tooltip("淡出时间（秒）")]
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("音效配置（可选）")]
    [Tooltip("打字音效ID（0表示不播放）")]
    [SerializeField] private int typingSoundId = 0;

    [Tooltip("每隔几个字符播放一次音效")]
    [SerializeField] private int soundPlayInterval = 3;

    [Header("事件回调")]
    [Tooltip("打字完成时的回调")]
    public UnityEvent onTypingComplete = new UnityEvent();

    [Tooltip("淡出完成时的回调")]
    public UnityEvent onFadeOutComplete = new UnityEvent();

    #endregion

    #region 私有字段

    private Text uiText;
    private TextMesh textMesh;
    private string fullText = "";
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool isCompleted = false;

    #endregion

    #region 生命周期

    private void Awake()
    {
        // 获取Text组件（UI Text）
        uiText = GetComponent<Text>();

        // 获取TextMesh组件（3D Text）
        textMesh = GetComponent<TextMesh>();

        if (uiText == null && textMesh == null)
        {
            Log.Error($"TypewriterEffect: GameObject '{gameObject.name}' 上没有找到 Text 或 TextMesh 组件！");
            enabled = false;
            return;
        }
    }


    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    private void OnDestroy()
    {
        // 清理协程
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 播放打字机效果
    /// </summary>
    public void Play()
    {
        // 获取要显示的文本
        fullText = GetDisplayText();

        if (string.IsNullOrEmpty(fullText))
        {
            Log.Warning($"TypewriterEffect: GameObject '{gameObject.name}' 的文本为空！");
            return;
        }

        // 停止之前的协程
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // 开始打字机效果
        typingCoroutine = StartCoroutine(TypewriterCoroutine());
    }

    /// <summary>
    /// 播放打字机效果（使用指定文本）
    /// </summary>
    public void Play(string text)
    {
        fullText = text;
        Play();
    }

    /// <summary>
    /// 停止打字机效果
    /// </summary>
    public void Stop()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        isTyping = false;
    }

    /// <summary>
    /// 设置是否在完成后淡出
    /// </summary>
    public void SetFadeOutAfterComplete(bool fadeOut, float duration = 1f, float waitTime = 1f)
    {
        fadeOutAfterComplete = fadeOut;
        fadeOutDuration = duration;
        waitTimeAfterComplete = waitTime;
    }
    /// <summary>
    /// 立即完成打字（跳过动画）
    /// </summary>
    public void Complete()
    {
        Stop();
        SetText(fullText);
        isCompleted = true;
        onTypingComplete?.Invoke();
    }

    /// <summary>
    /// 重置状态
    /// </summary>
    public void Reset()
    {
        Stop();
        SetText("");
        isCompleted = false;
        isTyping = false;
    }

    /// <summary>
    /// 设置打字速度
    /// </summary>
    public void SetTypeSpeed(float speed)
    {
        typeSpeed = Mathf.Max(0.001f, speed);
    }

    /// <summary>
    /// 设置多语言Key
    /// </summary>
    public void SetLocalizationKey(string key)
    {
        localizationKey = key;
    }

    /// <summary>
    /// 是否正在打字
    /// </summary>
    public bool IsTyping()
    {
        return isTyping;
    }

    /// <summary>
    /// 是否已完成
    /// </summary>
    public bool IsCompleted()
    {
        return isCompleted;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 获取要显示的文本
    /// </summary>
    private string GetDisplayText()
    {
        // 优先使用多语言Key
        if (!string.IsNullOrEmpty(localizationKey))
        {
            return GF.Localization.GetText(localizationKey);
        }

        // 否则使用组件上的初始文本
        if (uiText != null)
        {
            return uiText.text;
        }

        if (textMesh != null)
        {
            return textMesh.text;
        }

        return "";
    }

    /// <summary>
    /// 设置文本内容
    /// </summary>
    private void SetText(string text)
    {
        if (uiText != null)
        {
            uiText.text = text;
        }

        if (textMesh != null)
        {
            textMesh.text = text;
        }
    }

    /// <summary>
    /// 打字机效果协程
    /// </summary>
    private IEnumerator TypewriterCoroutine()
    {
        isTyping = true;
        isCompleted = false;

        // 清空文本
        SetText("");

        // 逐字显示
        int charCount = 0;
        foreach (char c in fullText)
        {
            SetText(GetText() + c);
            charCount++;

            // 播放打字音效
            if (typingSoundId > 0 && charCount % soundPlayInterval == 0)
            {
                // GF.Sound.PlayEffect(typingSoundId);
            }

            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
        isCompleted = true;

        Log.Info($"TypewriterEffect: 打字完成 - {gameObject.name}");

        // 触发完成回调
        onTypingComplete?.Invoke();

        // 等待一段时间
        if (waitTimeAfterComplete > 0)
        {
            yield return new WaitForSeconds(waitTimeAfterComplete);
        }

        // 淡出效果
        if (fadeOutAfterComplete)
        {
            yield return StartCoroutine(FadeOutCoroutine());
        }

        typingCoroutine = null;
    }

    /// <summary>
    /// 淡出效果协程
    /// </summary>
    private IEnumerator FadeOutCoroutine()
    {
        float elapsedTime = 0f;
        Color startColor = GetTextColor();
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeOutDuration;
            SetTextColor(Color.Lerp(startColor, targetColor, t));
            yield return null;
        }

        SetTextColor(targetColor);

        Log.Info($"TypewriterEffect: 淡出完成 - {gameObject.name}");

        // 触发淡出完成回调
        onFadeOutComplete?.Invoke();
    }

    /// <summary>
    /// 获取当前文本
    /// </summary>
    private string GetText()
    {
        if (uiText != null)
        {
            return uiText.text;
        }

        if (textMesh != null)
        {
            return textMesh.text;
        }

        return "";
    }

    /// <summary>
    /// 获取文本颜色
    /// </summary>
    private Color GetTextColor()
    {
        if (uiText != null)
        {
            return uiText.color;
        }

        if (textMesh != null)
        {
            return textMesh.color;
        }

        return Color.white;
    }

    /// <summary>
    /// 设置文本颜色
    /// </summary>
    private void SetTextColor(Color color)
    {
        if (uiText != null)
        {
            uiText.color = color;
        }

        if (textMesh != null)
        {
            textMesh.color = color;
        }
    }

    #endregion
}
