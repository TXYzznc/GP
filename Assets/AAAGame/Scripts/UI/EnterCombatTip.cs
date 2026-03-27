using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

/// <summary>
/// 进入战斗提示UI - 显示短暂提示后自动关闭
/// </summary>
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class EnterCombatTip : UIFormBase
{
    private float m_DisplayDuration = 1f; // 显示时长（秒）

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        // 解析参数
        if (userData is UIParams uiParams)
        {
            m_DisplayDuration = uiParams.Get<VarFloat>("DisplayDuration", 1f);
        }

        DebugEx.LogModule("EnterCombatTip", $"EnterCombatTip 打开，将在 {m_DisplayDuration} 秒后自动关闭");

        // 启动自动关闭计时
        StartAutoCloseTimer().Forget();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        DebugEx.LogModule("EnterCombatTip", "战斗进入提示UI关闭");
        base.OnClose(isShutdown, userData);
    }

    /// <summary>
    /// 自动关闭计时器
    /// </summary>
    private async UniTaskVoid StartAutoCloseTimer()
    {
        try
        {
            // 等待指定时长
            await UniTask.Delay((int)(m_DisplayDuration * 1000));

            // 检查UI是否还存在
            if (this == null || UIForm == null)
                return;

            DebugEx.LogModule("EnterCombatTip", $"显示时长已到 ({m_DisplayDuration}秒)，开始关闭UI");

            // 对于战斗进入提示这种简单UI，直接关闭避免动画延迟
            // 使用 CloseUIForm 而不是 Close，跳过关闭动画
            GF.UI.CloseUIForm(UIForm);
            
            DebugEx.LogModule("EnterCombatTip", "UI已直接关闭（跳过动画）");
        }
        catch (System.Exception ex)
        {
            DebugEx.Error("EnterCombatTip", $"自动关闭计时器异常: {ex.Message}");
        }
    }
}