using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 宝箱类可交互对象
/// 支持 Locked/Opened 两种状态
/// 首次交互时播放开箱动画，后续交互跳过动画直接打开宝箱界面
/// </summary>
[RequireComponent(typeof(OutlineController))]
public class TreasureChestInteractable : InteractableBase
{
    /// <summary>宝箱状态</summary>
    private enum ChestState
    {
        Locked,   // 未开启
        Opened    // 已开启
    }

    [Header("宝箱配置")]
    [SerializeField]
    [Tooltip("Animator Trigger 名称，用于触发开箱动画")]
    private string openAnimTrigger = "Open";

    [SerializeField]
    [Tooltip("显示交互提示时的描边颜色")]
    private Color outlineColor = new Color(1f, 0.85f, 0f); // OutlineController.SelectionColor

    [SerializeField]
    [Tooltip("显示交互提示时的描边宽度")]
    private float outlineSize = 1f; // OutlineController.DefaultSize

    private ChestState m_State = ChestState.Locked;
    private Animator m_Animator;
    private OutlineController m_OutlineController;
    private bool m_IsAnimating;

    public override string InteractionTip
    {
        get
        {
            return m_State == ChestState.Locked ? "打开宝箱" : "查看宝箱";
        }
    }

    /// <summary>始终返回 -1，不播放玩家侧交互动画</summary>
    public override int InteractAnimIndex => -1;

    /// <summary>交互时正在播放动画则返回 false，防止重复触发</summary>
    public override bool CanInteract(GameObject player) => !m_IsAnimating;

    /// <summary>执行交互逻辑</summary>
    public override void OnInteract(GameObject player)
    {
        OpenChestAsync().Forget();
    }

    protected override void Awake()
    {
        base.Awake();
        m_Animator = GetComponent<Animator>();
        m_OutlineController = GetComponent<OutlineController>();
    }

    /// <summary>成为/取消交互目标时控制描边</summary>
    public override void OnSetAsTarget(bool isTarget)
    {
        if (m_OutlineController == null) return;
        if (isTarget)
            m_OutlineController.ShowOutline(outlineColor, outlineSize);
        else
            m_OutlineController.HideOutline();
    }

    /// <summary>打开宝箱的异步流程</summary>
    private async UniTask OpenChestAsync()
    {
        m_IsAnimating = true;

        if (m_State == ChestState.Locked)
        {
            // 播放宝箱开箱动画（可选，若无 Animator 则跳过）
            if (m_Animator != null)
            {
                PlayOpenAnimation();
                await WaitForAnimationCompleteAsync();
            }

            // 转换状态
            m_State = ChestState.Opened;
        }

        m_IsAnimating = false;

        // 打开宝箱界面
        OpenChestUI();
    }

    /// <summary>触发开箱动画</summary>
    private void PlayOpenAnimation()
    {
        m_Animator.SetTrigger(openAnimTrigger);
    }

    /// <summary>等待 Animator 动画播放完成</summary>
    private async UniTask WaitForAnimationCompleteAsync()
    {
        // 等一帧让动画状态生效
        await UniTask.Yield(cancellationToken: this.GetCancellationTokenOnDestroy());

        // 等待 normalizedTime >= 1f（动画完成）
        await UniTask.WaitUntil(
            () =>
            {
                if (m_Animator == null || m_Animator.gameObject == null)
                    return true; // 对象已销毁，直接返回

                var stateInfo = m_Animator.GetCurrentAnimatorStateInfo(0);
                return stateInfo.normalizedTime >= 1f;
            },
            cancellationToken: this.GetCancellationTokenOnDestroy()
        );
    }

    /// <summary>打开宝箱界面（占位，等待 UI 实现）</summary>
    private void OpenChestUI()
    {
        // TODO: 宝箱界面未实现
        // 后续需要：
        // 1. 在 UITable 中注册宝箱界面表单
        // 2. 运行 DataTableGenerator 生成 Variables
        // 3. 创建 TreasureChestUIForm.cs，继承 UIFormLogic
        // 4. 此处调用：GF.UI.OpenUIForm(UIViews.TreasureChestUI);
        DebugEx.LogModule("TreasureChest", $"打开宝箱界面（占位）[State={m_State.ToString()}]");
    }
}
