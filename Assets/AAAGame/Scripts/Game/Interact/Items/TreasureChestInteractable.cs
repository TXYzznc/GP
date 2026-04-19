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

        // 从 Inspector 中获取 Lid 对象引用，并获取其 Animation 组件
        if (m_LidTransform != null)
        {
            m_Animation = m_LidTransform.GetComponent<Animation>();
        }
        else
        {
            DebugEx.Warning("TreasureChest", $"宝箱 [{m_TreasureBoxId}] 未设置 Lid Transform 引用");
        }

        // 使用反射获取 ChestEffectCycler，避免编译时类型检查问题
        var effectCyclerType = System.Type.GetType("ChestEffectCycler");
        if (effectCyclerType != null)
        {
            m_EffectCycler = GetComponentInChildren(effectCyclerType) as MonoBehaviour;
        }

        // 为这个宝箱创建独立的容器，用于存储物品列表
        m_Container = gameObject.AddComponent<TreasureBoxSlotContainerImpl>();

        // 初始化时禁用特效循环器，等待靠近时启用
        if (m_EffectCycler != null)
        {
            m_EffectCycler.enabled = false;
            DebugEx.Log("TreasureChest", $"宝箱 [{m_TreasureBoxId}] 特效循环器已禁用");
        }

        DebugEx.Log("TreasureChest", $"宝箱 [{m_TreasureBoxId}] 容器已创建");
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
        // 优先使用 Animator（如果存在且激活）
        if (m_Animator != null && m_Animator.enabled)
        {
            m_Animator.SetTrigger(openAnimTrigger);
            DebugEx.Log("TreasureChest", $"宝箱 [{m_TreasureBoxId}] 使用 Animator 播放开箱动画");
        }
        // 否则使用 Legacy Animation 组件
        else if (m_Animation != null && m_LidTransform != null)
        {
            m_Animation.Play("ChestOpen");
            DebugEx.Log("TreasureChest", $"宝箱 [{m_TreasureBoxId}] 使用 Animation 播放开箱动画");
        }
        else
        {
            DebugEx.Warning("TreasureChest", $"宝箱 [{m_TreasureBoxId}] 未找到有效的动画组件");
        }
    }

    /// <summary>等待 Animator 动画播放完成</summary>
    private async UniTask WaitForAnimationCompleteAsync()
    {
        // 等一帧让动画状态生效
        await UniTask.Yield(cancellationToken: this.GetCancellationTokenOnDestroy());

        // 使用 Animator 模式
        if (m_Animator != null && m_Animator.enabled)
        {
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
        // 使用 Legacy Animation 模式
        else if (m_Animation != null)
        {
            // 等待 Legacy Animation 播放完成
            await UniTask.WaitUntil(
                () =>
                {
                    if (m_Animation == null || m_LidTransform == null)
                        return true; // 对象已销毁，直接返回

                    return !m_Animation.isPlaying;
                },
                cancellationToken: this.GetCancellationTokenOnDestroy()
            );
        }
    }

    /// <summary>打开宝箱界面 - 首次打开时生成物品，后续直接打开 UI</summary>
    private void OpenChestUI()
    {
        // 首次打开时初始化容器内的物品
        if (!m_HasInitialized)
        {
            var initialItems = GenerateTreasureItems();
            if (initialItems == null)
                return; // 生成失败

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

        // 订阅 UI 关闭事件，用于播放关闭动画
        string treasureBoxUIAssetName = GF.UI.GetUIFormAssetName(UIViews.TreasureBoxUI);
        m_UIFormClosedHandler = (sender, e) =>
        {
            if (e is CloseUIFormCompleteEventArgs closeArgs)
            {
                if (closeArgs.UIFormAssetName == treasureBoxUIAssetName)
                {
                    PlayCloseAnimationAsync().Forget();
                    // 移除事件监听
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
            DebugEx.Log("TreasureChest", $"宝箱 [{m_TreasureBoxId}] 特效循环已禁用（已打开）");
        }

        DebugEx.LogModule(
            "TreasureChest",
            $"打开宝箱 [{m_TreasureBoxId}] {treasureBoxRow.Name} 稀有度={treasureBoxRow.Rarity}"
        );
    }

    /// <summary>播放关闭动画的异步流程</summary>
    private async UniTask PlayCloseAnimationAsync()
    {
        // 优先使用 Animator（如果存在且激活）
        if (m_Animator != null && m_Animator.enabled)
        {
            m_Animator.SetTrigger(closeAnimTrigger);
            DebugEx.Log("TreasureChest", $"宝箱 [{m_TreasureBoxId}] 使用 Animator 播放关闭动画");
        }
        // 否则使用 Legacy Animation 组件
        else if (m_Animation != null && m_LidTransform != null)
        {
            m_Animation.Play("ChestClose");
            DebugEx.Log("TreasureChest", $"宝箱 [{m_TreasureBoxId}] 使用 Animation 播放关闭动画");
        }
        else
        {
            DebugEx.Warning("TreasureChest", $"宝箱 [{m_TreasureBoxId}] 未找到有效的动画组件");
            return;
        }

        await WaitForAnimationCompleteAsync();

        DebugEx.Log("TreasureChest", $"宝箱 [{m_TreasureBoxId}] 关闭动画已完成");
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
            PlayerAccountDataManager.Instance?.AddGold(totalCoins);

            // 创建金币物品显示在宝箱格子里
            var coinItem = itemManager?.CreateItem(999); // 金币 ID=999
            if (coinItem != null)
            {
                items.Add(new ItemStack(coinItem, totalCoins));
                DebugEx.Log(
                    "TreasureChest",
                    $"[GenerateTreasureItems] 生成金币物品: {coinItem.Name} x{totalCoins}"
                );
            }

            // 刷新 CurrencyUI 显示
            string currencyUIAssetName = GF.UI.GetUIFormAssetName(UIViews.CurrencyUI);
            if (!string.IsNullOrEmpty(currencyUIAssetName))
            {
                var currencyUIForm = GF.UI.GetUIForm(currencyUIAssetName);
                var currencyUI = currencyUIForm?.Logic as CurrencyUI;
                if (currencyUI != null)
                {
                    currencyUI.RefreshCurrency();
                    DebugEx.Log("TreasureChest", "[GenerateTreasureItems] CurrencyUI 已刷新");
                }
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
            PlayerRuntimeDataManager.Instance?.AddSpiritStone(totalMagicaStone);

            // 创建灵石物品显示在宝箱格子里
            var spiritStoneItem = itemManager?.CreateItem(99999); // 灵石 ID=99999
            if (spiritStoneItem != null)
            {
                items.Add(new ItemStack(spiritStoneItem, totalMagicaStone));
                DebugEx.Log(
                    "TreasureChest",
                    $"[GenerateTreasureItems] 生成灵石物品: {spiritStoneItem.Name} x{totalMagicaStone}"
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
