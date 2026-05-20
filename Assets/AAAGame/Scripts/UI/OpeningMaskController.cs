using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 游戏开场遮罩控制器
/// 挂在 BuiltinViews/Mask 对象上
/// 游戏启动时停留一段时间后淡出，然后隐藏自身
/// </summary>
public class OpeningMaskController : MonoBehaviour
{
    #region 静态事件

    /// <summary>遮罩淡出完毕，通知 StartMenuUI 开始播放入场动画</summary>
    public static event System.Action OnMaskComplete;

    #endregion

    #region 字段

    [SerializeField, Tooltip("停留时间（秒）")]
    private float m_HoldDuration = 0.5f;

    [SerializeField, Tooltip("淡出时间（秒）")]
    private float m_FadeDuration = 1.0f;

    private CanvasGroup m_CanvasGroup;

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        m_CanvasGroup = GetComponent<CanvasGroup>();
        if (m_CanvasGroup == null)
        {
            DebugEx.Error(
                "OpeningMaskController",
                "未找到 CanvasGroup 组件，请在 Mask 对象上添加 CanvasGroup"
            );
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 播放开场淡出动画
    /// </summary>
    public async UniTask PlayAsync()
    {
        if (m_CanvasGroup == null)
            return;

        gameObject.SetActive(true);
        m_CanvasGroup.alpha = 1f;

        DebugEx.Log(
            "OpeningMaskController",
            $"开场遮罩开始，停留 {m_HoldDuration}s，淡出 {m_FadeDuration}s"
        );

        // 停留
        await UniTask.Delay((int)(m_HoldDuration * 1000));

        // 淡出
        var tcs = new UniTaskCompletionSource();
        m_CanvasGroup
            .DOFade(0f, m_FadeDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() => tcs.TrySetResult());
        await tcs.Task;

        // 隐藏
        gameObject.SetActive(false);

        DebugEx.Success("OpeningMaskController", "开场遮罩淡出完成");

        // 通知订阅者（如 StartMenuUI）开始播放入场动画
        OnMaskComplete?.Invoke();
    }

    #endregion
}
