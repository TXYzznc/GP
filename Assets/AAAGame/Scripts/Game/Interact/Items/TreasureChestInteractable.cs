using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

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
        Locked,   // 未开启
        Opened    // 已开启
    }

    [Header("宝箱配置")]
    [SerializeField]
    [Tooltip("宝箱 ID（用于从配置表读取数据）")]
    private int m_TreasureBoxId = 1;

    [SerializeField]
    [Tooltip("宝箱等级（决定开出物品数量概率，1-100）")]
    private int m_ChestLevel = 50;

    [SerializeField]
    [Tooltip("Animator Trigger 名称，用于触发开箱动画")]
    private string openAnimTrigger = "Open";

    [SerializeField]
    [Tooltip("显示交互提示时的描边颜色")]
    private Color outlineColor = new Color(1f, 0.85f, 0f);

    [SerializeField]
    [Tooltip("显示交互提示时的描边宽度")]
    private float outlineSize = 1f;

    private ChestState m_State = ChestState.Locked;
    private Animator m_Animator;
    private OutlineController m_OutlineController;
    private bool m_IsAnimating;
    private TreasureBoxSlotContainerImpl m_Container;  // 宝箱容器（内部存储物品列表）
    private bool m_HasInitialized = false;

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

        // 为这个宝箱创建独立的容器，用于存储物品列表
        m_Container = gameObject.AddComponent<TreasureBoxSlotContainerImpl>();
        DebugEx.Log("TreasureChest", $"宝箱 [{m_TreasureBoxId}] 容器已创建");
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

    /// <summary>打开宝箱界面 - 首次打开时生成物品，后续直接打开 UI</summary>
    private void OpenChestUI()
    {
        // 首次打开时初始化容器内的物品
        if (!m_HasInitialized)
        {
            var initialItems = GenerateTreasureItems();
            if (initialItems == null)
                return;  // 生成失败

            m_Container.Initialize(initialItems);
            m_HasInitialized = true;
        }

        // 读取宝箱配置获取名称
        var treasureBoxTable = GF.DataTable.GetDataTable<TreasureBoxTable>();
        if (treasureBoxTable == null)
        {
            DebugEx.Error("TreasureChest", "TreasureBoxTable 未加载");
            return;
        }

        var treasureBoxRow = treasureBoxTable.GetDataRow(m_TreasureBoxId);
        if (treasureBoxRow == null)
        {
            DebugEx.Error("TreasureChest", $"宝箱 ID {m_TreasureBoxId} 不存在");
            return;
        }

        // 打开宝箱 UI（传递容器引用，UI 将直接从容器读取数据）
        var uiParams = UIParams.Create();
        uiParams.Set("TreasureBoxContainer", m_Container);
        uiParams.Set("TreasureBoxName", treasureBoxRow.Name);
        GF.UI.OpenUIForm(UIViews.TreasureBoxUI, uiParams);

        DebugEx.LogModule("TreasureChest",
            $"打开宝箱 [{m_TreasureBoxId}] {treasureBoxRow.Name} 稀有度={treasureBoxRow.Rarity}");
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

        // 从 ItemGroupTable 读取物品列表
        var itemGroupTable = GF.DataTable.GetDataTable<ItemGroupTable>();
        if (itemGroupTable == null)
        {
            DebugEx.Error("TreasureChest", "ItemGroupTable 未加载");
            return null;
        }

        var itemGroupRow = itemGroupTable.GetDataRow(treasureBoxRow.ItemGroupId);
        if (itemGroupRow == null)
        {
            DebugEx.Error("TreasureChest", $"物品组 ID {treasureBoxRow.ItemGroupId} 不存在");
            return null;
        }

        // 获取物品 ID 数组
        var itemIds = itemGroupRow.ItemIds;
        if (itemIds == null || itemIds.Length == 0)
        {
            DebugEx.Warning("TreasureChest", $"物品组 {treasureBoxRow.ItemGroupId} 为空");
            return items;  // 返回空列表
        }

        // 根据宝箱等级计算开出物品数量
        int itemCount = CalculateItemCount(treasureBoxRow.ItemCountMin, treasureBoxRow.ItemCountMax, m_ChestLevel);

        // 随机从物品列表中选择物品
        for (int i = 0; i < itemCount; i++)
        {
            int randomIndex = Random.Range(0, itemIds.Length);
            int itemId = itemIds[randomIndex];

            var item = itemManager?.CreateItem(itemId);
            if (item != null)
                items.Add(new ItemStack(item, 1));
        }

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
}
