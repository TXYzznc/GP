using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 宝箱类可交互对象
/// 支持 Locked/Opened 两种状态
/// 首次交互时播放开箱动画，后续交互跳过动画直接打开宝箱界面
/// 根据配置表生成随机物品列表
/// </summary>
[RequireComponent(typeof(OutlineController))]
public class TreasureChestInteractable : InteractableBase
{
    /// <summary>宝箱状态</summary>
    private enum ChestState
    {
        Locked, // 未开启
        Opened, // 已开启
    }

    [Header("宝箱配置")]
    [SerializeField]
    [Tooltip("宝箱 ID（用于从配置表读取数据）")]
    private int m_TreasureBoxId = 1;

    [SerializeField]
    [Tooltip("宝箱等级（决定开出物品数量概率，1-100）")]
    private int m_ChestLevel = 50;

    [SerializeField]
    [Tooltip("Lid 子对象（用于播放动画）")]
    private Transform m_LidTransform;

    [SerializeField]
    [Tooltip("Animator Trigger 名称，用于触发开箱动画")]
    private string openAnimTrigger = "Open";

    [SerializeField]
    [Tooltip("Animator Trigger 名称，用于触发关箱动画")]
    private string closeAnimTrigger = "Close";

    [SerializeField]
    [Tooltip("Legacy Animation 动画片段数组 - [0]=Open动画, [1]=Close动画")]
    private AnimationClip[] m_AnimationClips = new AnimationClip[2];

    [SerializeField]
    [Tooltip("显示交互提示时的描边颜色")]
    private Color outlineColor = new Color(1f, 0.85f, 0f);

    [SerializeField]
    [Tooltip("显示交互提示时的描边宽度")]
    private float outlineSize = 1f;

    private ChestState m_State = ChestState.Locked;
    private Animator m_Animator;
    private Animation m_Animation; // Legacy Animation 组件
    private OutlineController m_OutlineController;
    private MonoBehaviour m_EffectCycler; // 改为 MonoBehaviour，避免编译时类型检查
    private bool m_IsAnimating;
    private TreasureBoxSlotContainerImpl m_Container; // 宝箱容器（内部存储物品列表）
    private bool m_HasInitialized = false;
    private System.EventHandler<GameEventArgs> m_UIFormClosedHandler;

    public override string InteractionTip
    {
        get { return m_State == ChestState.Locked ? "打开宝箱" : "查看宝箱"; }
    }

    /// <summary>始终返回 -1，不播放玩家侧交互动画</summary>
    public override int InteractAnimIndex => -1;

    /// <summary>交互时正在播放动画或已开始交互则返回 false，防止重复触发</summary>
    public override bool CanInteract(GameObject player) => !m_IsAnimating && !m_HasStartedInteraction;

    /// <summary>执行交互逻辑</summary>
    public override void OnInteract(GameObject player)
    {
        SetInteractionStarted(true);  // 基类会自动隐藏描边
        OpenChestAsync().Forget();
    }

    /// <summary>
    /// 设置宝箱配置（运行时动态生成用）
    /// 必须在 Awake 和 Start 之间调用
    /// </summary>
    public void SetTreasureBoxData(int treasureBoxId, int chestLevel)
    {
        m_TreasureBoxId = treasureBoxId;
        m_ChestLevel = Mathf.Clamp(chestLevel, 1, 100);
    }

    protected override void Awake()
    {
        base.Awake();

        DebugEx.Log("TreasureChest", $"[Awake] 宝箱 [{m_TreasureBoxId}] 初始化开始");

        m_Animator = GetComponent<Animator>();
        bool animatorEnabled = m_Animator != null && m_Animator.enabled;
        DebugEx.Log("TreasureChest", $"[Awake] m_Animator={m_Animator}, enabled={animatorEnabled}");

        m_OutlineController = GetComponent<OutlineController>();
        DebugEx.Log("TreasureChest", $"[Awake] m_OutlineController={m_OutlineController}");

        // 从 Inspector 中获取 Lid 对象引用，并获取其 Animation 组件
        if (m_LidTransform != null)
        {
            m_Animation = m_LidTransform.GetComponent<Animation>();
            DebugEx.Log("TreasureChest", $"[Awake] m_LidTransform设置，m_Animation={m_Animation}");

            // 验证动画数组是否配置
            if (m_AnimationClips == null || m_AnimationClips.Length < 2 || m_AnimationClips[0] == null || m_AnimationClips[1] == null)
            {
                DebugEx.Warning("TreasureChest", $"[Awake] 动画数组未配置或元素为空。请在 Inspector 中设置：[0]=Open动画, [1]=Close动画");
            }
            else
            {
                DebugEx.Log("TreasureChest", $"[Awake] 动画片段已配置: Open={m_AnimationClips[0].name}, Close={m_AnimationClips[1].name}");
            }
        }
        else
        {
            DebugEx.Warning("TreasureChest", $"[Awake] 宝箱 [{m_TreasureBoxId}] 未设置 Lid Transform 引用");
        }

        // 使用反射获取 ChestEffectCycler，避免编译时类型检查问题
        var effectCyclerType = System.Type.GetType("ChestEffectCycler");
        if (effectCyclerType != null)
        {
            m_EffectCycler = GetComponentInChildren(effectCyclerType) as MonoBehaviour;
            DebugEx.Log("TreasureChest", $"[Awake] m_EffectCycler={m_EffectCycler}");
        }
        else
        {
            DebugEx.Warning("TreasureChest", $"[Awake] 找不到ChestEffectCycler类型");
        }

        // 为这个宝箱创建独立的容器，用于存储物品列表
        m_Container = gameObject.AddComponent<TreasureBoxSlotContainerImpl>();
        DebugEx.Log("TreasureChest", $"[Awake] 容器已创建");

        // 初始化时禁用特效循环器，等待靠近时启用
        if (m_EffectCycler != null)
        {
            m_EffectCycler.enabled = false;
            DebugEx.Log("TreasureChest", $"[Awake] 特效循环器已禁用");
        }

        DebugEx.Success("TreasureChest", $"[Awake] 宝箱 [{m_TreasureBoxId}] 初始化完成");
    }

    private void OnDestroy()
    {
        // 清理事件监听
        if (m_UIFormClosedHandler != null)
        {
            GF.Event.Unsubscribe(CloseUIFormCompleteEventArgs.EventId, m_UIFormClosedHandler);
        }
    }

    /// <summary>成为/取消交互目标时控制描边和特效</summary>
    public override void OnSetAsTarget(bool isTarget)
    {
        if (m_OutlineController == null)
            return;

        if (isTarget)
        {
            m_OutlineController.ShowOutline(outlineColor, outlineSize);
            // 靠近时启用特效循环（仅在未开启时）
            if (m_State == ChestState.Locked && m_EffectCycler != null)
            {
                m_EffectCycler.enabled = true;
                DebugEx.Log("TreasureChest", $"宝箱 [{m_TreasureBoxId}] 特效循环已启用");
            }
        }
        else
        {
            m_OutlineController.HideOutline();
            // 离开时禁用特效循环
            if (m_EffectCycler != null)
            {
                m_EffectCycler.enabled = false;
                DebugEx.Log("TreasureChest", $"宝箱 [{m_TreasureBoxId}] 特效循环已禁用");
            }
        }
    }

    /// <summary>打开宝箱的异步流程</summary>
    private async UniTask OpenChestAsync()
    {
        DebugEx.Log("TreasureChest", $"[OpenChestAsync] 宝箱 [{m_TreasureBoxId}] 开箱流程开始");
        m_IsAnimating = true;

        if (m_State == ChestState.Locked)
        {
            DebugEx.Log("TreasureChest", $"[OpenChestAsync] 宝箱 [{m_TreasureBoxId}] 状态为Locked，准备播放Open动画");

            // 检查是否有动画组件（Animator 或 Legacy Animation）
            bool hasAnimator = m_Animator != null;
            bool hasLegacyAnimation = m_Animation != null && m_LidTransform != null;

            if (hasAnimator || hasLegacyAnimation)
            {
                DebugEx.Log("TreasureChest", $"[OpenChestAsync] 宝箱 [{m_TreasureBoxId}] 检测到动画组件，开始播放Open动画");
                PlayOpenAnimation();
                DebugEx.Log("TreasureChest", $"[OpenChestAsync] 宝箱 [{m_TreasureBoxId}] PlayOpenAnimation已调用");

                // 延迟 0.3 秒后打开 UI（动画继续播放）
                await UniTask.Delay(300, cancellationToken: this.GetCancellationTokenOnDestroy());
                DebugEx.Log("TreasureChest", $"[OpenChestAsync] 宝箱 [{m_TreasureBoxId}] 延迟 0.3 秒完成，准备打开UI");

                // 打开宝箱界面
                OpenChestUI();

                // 继续等待动画完全播放完成
                await WaitForAnimationCompleteAsync();
                DebugEx.Success("TreasureChest", $"[OpenChestAsync] 宝箱 [{m_TreasureBoxId}] Open动画已完成");
            }
            else
            {
                DebugEx.Warning("TreasureChest",
                    $"[OpenChestAsync] 宝箱 [{m_TreasureBoxId}] 没有有效的动画组件(m_Animator={m_Animator}, m_Animation={m_Animation}, m_LidTransform={m_LidTransform})，跳过Open动画");
                // 没有动画的情况下也要打开 UI
                OpenChestUI();
            }

            // 转换状态
            m_State = ChestState.Opened;
            DebugEx.Log("TreasureChest", $"[OpenChestAsync] 宝箱 [{m_TreasureBoxId}] 状态已转换为Opened");
        }
        else
        {
            DebugEx.Log("TreasureChest", $"[OpenChestAsync] 宝箱 [{m_TreasureBoxId}] 状态不是Locked（已打开过），跳过Open动画");
            // 已打开过的情况下直接打开 UI
            OpenChestUI();
        }

        m_IsAnimating = false;
    }

    /// <summary>触发开箱动画</summary>
    private void PlayOpenAnimation()
    {
        DebugEx.Log("TreasureChest", $"[PlayOpenAnimation] 宝箱 [{m_TreasureBoxId}] 开始播放Open动画");
        bool animatorEnabled = m_Animator != null && m_Animator.enabled;
        DebugEx.Log("TreasureChest", $"[PlayOpenAnimation] m_Animator={m_Animator}, m_Animator.enabled={animatorEnabled}, openAnimTrigger={openAnimTrigger}");
        DebugEx.Log("TreasureChest", $"[PlayOpenAnimation] m_Animation={m_Animation}, m_LidTransform={m_LidTransform}");

        // 优先使用 Animator（如果存在且激活）
        if (m_Animator != null && m_Animator.enabled)
        {
            DebugEx.Success("TreasureChest", $"[PlayOpenAnimation] 宝箱 [{m_TreasureBoxId}] 使用Animator，触发Trigger: {openAnimTrigger}");
            m_Animator.SetTrigger(openAnimTrigger);
            DebugEx.Log("TreasureChest", $"[PlayOpenAnimation] 宝箱 [{m_TreasureBoxId}] Trigger已设置，检查动画参数");

            // 输出当前动画状态
            var stateInfo = m_Animator.GetCurrentAnimatorStateInfo(0);
            DebugEx.Log("TreasureChest", $"[PlayOpenAnimation] 当前动画状态: {stateInfo.shortNameHash}, normalizedTime={stateInfo.normalizedTime}");
        }
        // 否则使用 Legacy Animation 组件
        else if (m_Animation != null && m_LidTransform != null)
        {
            // 使用数组索引访问 Open 动画（第一个元素）
            if (m_AnimationClips != null && m_AnimationClips.Length > 0 && m_AnimationClips[0] != null)
            {
                DebugEx.Success("TreasureChest", $"[PlayOpenAnimation] 宝箱 [{m_TreasureBoxId}] 使用LegacyAnimation，播放: {m_AnimationClips[0].name}");
                m_Animation.clip = m_AnimationClips[0];
                m_Animation.Play();
                DebugEx.Log("TreasureChest", $"[PlayOpenAnimation] 动画已播放，isPlaying={m_Animation.isPlaying}");
            }
            else
            {
                DebugEx.Error("TreasureChest", $"[PlayOpenAnimation] 宝箱 [{m_TreasureBoxId}] 动画数组未配置或Open动画为空");
            }
        }
        else
        {
            DebugEx.Error("TreasureChest",
                $"[PlayOpenAnimation] 宝箱 [{m_TreasureBoxId}] 动画播放失败! m_Animator={m_Animator}, m_Animation={m_Animation}, m_LidTransform={m_LidTransform}");
            if (m_Animator != null && !m_Animator.enabled)
            {
                DebugEx.Error("TreasureChest", $"[PlayOpenAnimation] Animator存在但被禁用!");
            }
        }
    }

    /// <summary>等待 Animator 动画播放完成</summary>
    private async UniTask WaitForAnimationCompleteAsync()
    {
        DebugEx.Log("TreasureChest", $"[WaitForAnimationCompleteAsync] 宝箱 [{m_TreasureBoxId}] 等待动画完成开始");

        // 等一帧让动画状态生效
        await UniTask.Yield(cancellationToken: this.GetCancellationTokenOnDestroy());
        DebugEx.Log("TreasureChest", $"[WaitForAnimationCompleteAsync] 已等待一帧");

        // 使用 Animator 模式
        if (m_Animator != null && m_Animator.enabled)
        {
            DebugEx.Log("TreasureChest", $"[WaitForAnimationCompleteAsync] 使用Animator模式等待动画完成");

            int checkCount = 0;
            // 等待 normalizedTime >= 1f（动画完成）
            await UniTask.WaitUntil(
                () =>
                {
                    if (m_Animator == null || m_Animator.gameObject == null)
                    {
                        DebugEx.Warning("TreasureChest", $"[WaitForAnimationCompleteAsync] Animator对象已销毁");
                        return true;
                    }

                    var stateInfo = m_Animator.GetCurrentAnimatorStateInfo(0);
                    checkCount++;

                    if (checkCount % 10 == 0)
                        DebugEx.Log("TreasureChest", $"[WaitForAnimationCompleteAsync] 检查动画状态 ({checkCount}次): normalizedTime={stateInfo.normalizedTime:F3}");

                    return stateInfo.normalizedTime >= 1f;
                },
                cancellationToken: this.GetCancellationTokenOnDestroy()
            );

            DebugEx.Success("TreasureChest", $"[WaitForAnimationCompleteAsync] Animator动画已完成，共检查{checkCount}次");
        }
        // 使用 Legacy Animation 模式
        else if (m_Animation != null)
        {
            DebugEx.Log("TreasureChest", $"[WaitForAnimationCompleteAsync] 使用LegacyAnimation模式等待动画完成");

            int checkCount = 0;
            // 等待 Legacy Animation 播放完成
            await UniTask.WaitUntil(
                () =>
                {
                    if (m_Animation == null || m_LidTransform == null)
                    {
                        DebugEx.Warning("TreasureChest", $"[WaitForAnimationCompleteAsync] Animation对象已销毁");
                        return true;
                    }

                    checkCount++;
                    bool isPlaying = m_Animation.isPlaying;

                    if (checkCount % 10 == 0)
                        DebugEx.Log("TreasureChest", $"[WaitForAnimationCompleteAsync] 检查动画状态 ({checkCount}次): isPlaying={isPlaying}");

                    return !isPlaying;
                },
                cancellationToken: this.GetCancellationTokenOnDestroy()
            );

            DebugEx.Success("TreasureChest", $"[WaitForAnimationCompleteAsync] LegacyAnimation动画已完成，共检查{checkCount}次");
        }
        else
        {
            DebugEx.Error("TreasureChest", $"[WaitForAnimationCompleteAsync] 宝箱 [{m_TreasureBoxId}] 没有有效的动画组件!");
        }
    }

    /// <summary>打开宝箱界面 - 首次打开时生成物品，后续直接打开 UI</summary>
    private void OpenChestUI()
    {
        DebugEx.Log("TreasureChest", $"[OpenChestUI] 宝箱 [{m_TreasureBoxId}] 打开UI流程开始");

        // 首次打开时初始化容器内的物品
        if (!m_HasInitialized)
        {
            DebugEx.Log("TreasureChest", $"[OpenChestUI] 宝箱 [{m_TreasureBoxId}] 首次打开，生成物品");
            var initialItems = GenerateTreasureItems();
            if (initialItems == null)
            {
                DebugEx.Error("TreasureChest", $"[OpenChestUI] 宝箱 [{m_TreasureBoxId}] 物品生成失败");
                return;
            }

            m_Container.Initialize(initialItems);
            m_HasInitialized = true;
            DebugEx.Log("TreasureChest", $"[OpenChestUI] 宝箱 [{m_TreasureBoxId}] 容器初始化完成");
        }

        // 读取宝箱配置获取名称
        var treasureBoxTable = GF.DataTable.GetDataTable<TreasureBoxTable>();
        if (treasureBoxTable == null)
        {
            DebugEx.Error("TreasureChest", "[OpenChestUI] TreasureBoxTable 未加载");
            return;
        }

        var treasureBoxRow = treasureBoxTable.GetDataRow(m_TreasureBoxId);
        if (treasureBoxRow == null)
        {
            DebugEx.Error("TreasureChest", $"[OpenChestUI] 宝箱 ID {m_TreasureBoxId} 不存在");
            return;
        }

        // 打开宝箱 UI（传递容器引用，UI 将直接从容器读取数据）
        DebugEx.Log("TreasureChest", $"[OpenChestUI] 宝箱 [{m_TreasureBoxId}] 打开TreasureBoxUI");
        var uiParams = UIParams.Create();
        uiParams.Set("TreasureBoxContainer", m_Container);
        uiParams.Set("TreasureBoxName", treasureBoxRow.Name);
        GF.UI.OpenUIForm(UIViews.TreasureBoxUI, uiParams);

        // 订阅 UI 关闭事件，用于播放关闭动画
        string treasureBoxUIAssetName = GF.UI.GetUIFormAssetName(UIViews.TreasureBoxUI);
        DebugEx.Log("TreasureChest", $"[OpenChestUI] 宝箱 [{m_TreasureBoxId}] 订阅UI关闭事件，UIAssetName={treasureBoxUIAssetName}");

        m_UIFormClosedHandler = (sender, e) =>
        {
            if (e is CloseUIFormCompleteEventArgs closeArgs)
            {
                DebugEx.Log("TreasureChest", $"[UIFormClosedHandler] 收到UI关闭事件，UIAssetName={closeArgs.UIFormAssetName}");

                if (closeArgs.UIFormAssetName == treasureBoxUIAssetName)
                {
                    DebugEx.Log("TreasureChest", $"[UIFormClosedHandler] 宝箱 [{m_TreasureBoxId}] UI已关闭，开始播放Close动画");
                    PlayCloseAnimationAsync().Forget();

                    // 移除事件监听
                    DebugEx.Log("TreasureChest", $"[UIFormClosedHandler] 宝箱 [{m_TreasureBoxId}] 取消订阅UI关闭事件");
                    GF.Event.Unsubscribe(
                        CloseUIFormCompleteEventArgs.EventId,
                        m_UIFormClosedHandler
                    );
                    m_UIFormClosedHandler = null;
                }
            }
        };
        GF.Event.Subscribe(CloseUIFormCompleteEventArgs.EventId, m_UIFormClosedHandler);

        // 禁用特效循环（宝箱已打开）
        if (m_EffectCycler != null)
        {
            m_EffectCycler.enabled = false;
            DebugEx.Log("TreasureChest", $"[OpenChestUI] 宝箱 [{m_TreasureBoxId}] 特效循环已禁用（已打开）");
        }

        DebugEx.Success(
            "TreasureChest",
            $"[OpenChestUI] 宝箱 [{m_TreasureBoxId}] {treasureBoxRow.Name} (稀有度={treasureBoxRow.Rarity}) UI已打开"
        );
    }

    /// <summary>播放关闭动画的异步流程</summary>
    private async UniTask PlayCloseAnimationAsync()
    {
        DebugEx.Log("TreasureChest", $"[PlayCloseAnimationAsync] 宝箱 [{m_TreasureBoxId}] 播放Close动画开始");

        bool animatorEnabled = m_Animator != null && m_Animator.enabled;
        DebugEx.Log("TreasureChest", $"[PlayCloseAnimationAsync] m_Animator={m_Animator}, m_Animator.enabled={animatorEnabled}, closeAnimTrigger={closeAnimTrigger}");
        DebugEx.Log("TreasureChest", $"[PlayCloseAnimationAsync] m_Animation={m_Animation}, m_LidTransform={m_LidTransform}");

        // 优先使用 Animator（如果存在且激活）
        if (m_Animator != null && m_Animator.enabled)
        {
            DebugEx.Success("TreasureChest", $"[PlayCloseAnimationAsync] 宝箱 [{m_TreasureBoxId}] 使用Animator，触发Trigger: {closeAnimTrigger}");
            m_Animator.SetTrigger(closeAnimTrigger);

            var stateInfo = m_Animator.GetCurrentAnimatorStateInfo(0);
            DebugEx.Log("TreasureChest", $"[PlayCloseAnimationAsync] 当前动画状态: {stateInfo.shortNameHash}, normalizedTime={stateInfo.normalizedTime}");
        }
        // 否则使用 Legacy Animation 组件
        else if (m_Animation != null && m_LidTransform != null)
        {
            // 使用数组索引访问 Close 动画（第二个元素）
            if (m_AnimationClips != null && m_AnimationClips.Length > 1 && m_AnimationClips[1] != null)
            {
                DebugEx.Success("TreasureChest", $"[PlayCloseAnimationAsync] 宝箱 [{m_TreasureBoxId}] 使用LegacyAnimation，播放: {m_AnimationClips[1].name}");
                m_Animation.clip = m_AnimationClips[1];
                m_Animation.Play();
                DebugEx.Log("TreasureChest", $"[PlayCloseAnimationAsync] 动画已播放，isPlaying={m_Animation.isPlaying}");
            }
            else
            {
                DebugEx.Error("TreasureChest", $"[PlayCloseAnimationAsync] 宝箱 [{m_TreasureBoxId}] 动画数组未配置或Close动画为空");
            }
        }
        else
        {
            DebugEx.Error("TreasureChest",
                $"[PlayCloseAnimationAsync] 宝箱 [{m_TreasureBoxId}] 动画播放失败! m_Animator={m_Animator}, m_Animation={m_Animation}, m_LidTransform={m_LidTransform}");
            return;
        }

        DebugEx.Log("TreasureChest", $"[PlayCloseAnimationAsync] 宝箱 [{m_TreasureBoxId}] 等待Close动画完成");
        await WaitForAnimationCompleteAsync();

        // Close 动画播放完成后，重置宝箱状态为 Locked，允许下次再次打开
        // 注：交互标记不在这里重置，要等玩家离开范围时才重置
        m_State = ChestState.Locked;
        DebugEx.Success("TreasureChest", $"[PlayCloseAnimationAsync] 宝箱 [{m_TreasureBoxId}] Close动画已完成，状态已重置为Locked");
    }

    /// <summary>生成宝箱物品列表（只在第一次调用，后续由缓存复用）</summary>
    private List<ItemStack> GenerateTreasureItems()
    {
        var items = new List<ItemStack>();
        var itemManager = ItemManager.Instance;

        // 从 TreasureBoxTable 读取宝箱配置
        var treasureBoxTable = GF.DataTable.GetDataTable<TreasureBoxTable>();
        if (treasureBoxTable == null)
        {
            DebugEx.Error("TreasureChest", "TreasureBoxTable 未加载");
            return null;
        }

        var treasureBoxRow = treasureBoxTable.GetDataRow(m_TreasureBoxId);
        if (treasureBoxRow == null)
        {
            DebugEx.Error("TreasureChest", $"宝箱 ID {m_TreasureBoxId} 不存在");
            return null;
        }

        // 检查物品组ID数组
        var itemGroupIds = treasureBoxRow.ItemGroupIds;
        if (itemGroupIds == null || itemGroupIds.Length == 0)
        {
            DebugEx.Error("TreasureChest", $"宝箱 ID {m_TreasureBoxId} 没有配置物品组");
            return null;
        }

        // 从 ItemGroupTable 读取物品列表
        var itemGroupTable = GF.DataTable.GetDataTable<ItemGroupTable>();
        if (itemGroupTable == null)
        {
            DebugEx.Error("TreasureChest", "ItemGroupTable 未加载");
            return null;
        }

        // 合并所有物品组的物品ID
        var allItemIds = new List<int>();
        foreach (var groupId in itemGroupIds)
        {
            var itemGroupRow = itemGroupTable.GetDataRow(groupId);
            if (itemGroupRow == null)
            {
                DebugEx.Warning("TreasureChest", $"物品组 ID {groupId} 不存在，跳过");
                continue;
            }

            var itemIds = itemGroupRow.ItemIds;
            if (itemIds != null && itemIds.Length > 0)
            {
                allItemIds.AddRange(itemIds);
                DebugEx.Log(
                    "TreasureChest",
                    $"[GenerateTreasureItems] 物品组 {groupId} 添加 {itemIds.Length} 个物品"
                );
            }
            else
            {
                DebugEx.Warning("TreasureChest", $"物品组 {groupId} 为空");
            }
        }

        // 检查合并后的物品列表
        if (allItemIds.Count == 0)
        {
            DebugEx.Warning("TreasureChest", $"宝箱 ID {m_TreasureBoxId} 的所有物品组都为空");
            return items; // 返回空列表
        }

        // 根据宝箱等级计算开出物品数量
        int itemCount = CalculateItemCount(
            treasureBoxRow.ItemCountMin,
            treasureBoxRow.ItemCountMax,
            m_ChestLevel
        );

        DebugEx.Log(
            "TreasureChest",
            $"[GenerateTreasureItems] 宝箱 {m_TreasureBoxId} 生成 {itemCount} 个物品，总物品池大小 {allItemIds.Count}"
        );

        // 随机从合并的物品列表中选择物品
        for (int i = 0; i < itemCount; i++)
        {
            int randomIndex = Random.Range(0, allItemIds.Count);
            int itemId = allItemIds[randomIndex];

            var item = itemManager?.CreateItem(itemId);
            if (item != null)
            {
                items.Add(new ItemStack(item, 1));
                DebugEx.Log(
                    "TreasureChest",
                    $"[GenerateTreasureItems] 生成物品: {item.Name} (ID: {itemId})"
                );
            }
            else
            {
                DebugEx.Warning(
                    "TreasureChest",
                    $"[GenerateTreasureItems] 无法创建物品 ID {itemId}"
                );
            }
        }

        // 处理金币掉落
        int totalCoins = 0;
        if (Random.value < treasureBoxRow.CoinsProbability)
        {
            totalCoins = Random.Range(treasureBoxRow.MinCoins, treasureBoxRow.MaxCoins + 1);
            // 不在生成时添加金币，等待用户右键点击时再添加

            // 创建金币物品显示在宝箱格子里（虚拟物品，只用于显示）
            var coinItem = itemManager?.CreateItem(999); // 金币 ID=999
            if (coinItem != null)
            {
                items.Add(new ItemStack(coinItem, totalCoins));
                DebugEx.Log(
                    "TreasureChest",
                    $"[GenerateTreasureItems] 生成金币物品: {coinItem.Name} x{totalCoins}（虚拟物品，待用户获取）"
                );
            }
        }

        // 处理灵石掉落（连续抽奖机制）
        int totalMagicaStone = treasureBoxRow.MinMagicaStone; // 先加上保底数量

        // 连续抽奖：每次都有概率获得灵石，直到抽出不是灵石
        while (Random.value < treasureBoxRow.MagicaStoneProbability)
        {
            totalMagicaStone++;

            // 检查是否达到上限
            if (totalMagicaStone >= treasureBoxRow.MaxMagicaStone)
            {
                totalMagicaStone = treasureBoxRow.MaxMagicaStone;
                break;
            }
        }

        if (totalMagicaStone > 0)
        {
            // 不在生成时添加灵石，等待用户右键点击时再添加

            // 创建灵石物品显示在宝箱格子里（虚拟物品，只用于显示）
            var spiritStoneItem = itemManager?.CreateItem(99999); // 灵石 ID=99999
            if (spiritStoneItem != null)
            {
                items.Add(new ItemStack(spiritStoneItem, totalMagicaStone));
                DebugEx.Log(
                    "TreasureChest",
                    $"[GenerateTreasureItems] 生成灵石物品: {spiritStoneItem.Name} x{totalMagicaStone}（虚拟物品，待用户获取）"
                );
            }
        }

        // 打印宝箱掉落统计
        PrintTreasureBoxLoot(treasureBoxRow.Name, items, totalCoins, totalMagicaStone);

        return items;
    }

    /// <summary>
    /// 根据宝箱等级计算开出物品数量
    /// 等级越高，开出最大值的概率越大
    /// 等级=100时，开出最大值概率为0.9
    /// 等级=1时，开出最大值概率为0.1
    /// </summary>
    private int CalculateItemCount(int minCount, int maxCount, int level)
    {
        // 钳制等级到 1-100
        level = Mathf.Clamp(level, 1, 100);

        // 计算开出最大值的概率（从 0.1 到 0.9）
        float maxProbability = 0.1f + (level - 1) * 0.8f / 99f;

        // 随机决定是否开出最大值
        if (Random.value < maxProbability)
            return maxCount;
        else
            return Random.Range(minCount, maxCount);
    }

    /// <summary>
    /// 打印宝箱掉落统计信息
    /// </summary>
    private void PrintTreasureBoxLoot(
        string treasureBoxName,
        List<ItemStack> items,
        int coins,
        int magicaStone
    )
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("═══════════════════════════════════════════════════════");
        sb.AppendLine($"【宝箱掉落统计】{treasureBoxName}");
        sb.AppendLine("═══════════════════════════════════════════════════════");

        // 物品统计
        if (items != null && items.Count > 0)
        {
            sb.AppendLine($"📦 物品 ({items.Count} 个):");
            foreach (var itemStack in items)
            {
                if (itemStack?.Item != null)
                {
                    sb.AppendLine($"   • {itemStack.Item.Name} x{itemStack.Count}");
                }
            }
        }
        else
        {
            sb.AppendLine("📦 物品: 无");
        }

        // 金币统计
        if (coins > 0)
        {
            sb.AppendLine($"💰 金币: +{coins}");
        }
        else
        {
            sb.AppendLine("💰 金币: 无");
        }

        // 灵石统计
        if (magicaStone > 0)
        {
            sb.AppendLine($"✨ 灵石: +{magicaStone}");
        }
        else
        {
            sb.AppendLine("✨ 灵石: 无");
        }

        sb.AppendLine("═══════════════════════════════════════════════════════");

        DebugEx.Log("TreasureChest", sb.ToString());
    }
}
