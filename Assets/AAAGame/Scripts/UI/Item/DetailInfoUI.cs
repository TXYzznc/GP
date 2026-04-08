using System;
using UnityEngine;
using UnityEngine.UI;
using GameExtension;
using Cysharp.Threading.Tasks;
using UnityGameFramework.Runtime;
using DG.Tweening;

public partial class DetailInfoUI : UIItemBase
{
    #region 常量

    private const float SLIDE_IN_DURATION = 0.4f;
    private const float INITIAL_X = 360f;
    private const float TARGET_X = 0f;

    #endregion

    #region 字段

    private CardData m_CardData;
    private ChessEntity m_ChessEntity;
    private SummonChessConfig m_ChessConfig;
    private GlobalChessState m_GlobalState;
    private RectTransform m_RectTransform;
    private Tween m_SlideInTween;
    private System.Collections.Generic.Dictionary<int, BuffItem> m_BuffItems = new System.Collections.Generic.Dictionary<int, BuffItem>();
    private int m_CurrentMode = 0; // 0=卡牌, 1=棋子实体（战斗阶段）, 2=棋子配置（准备阶段）

    // 装备系统
    private int m_CurrentChessId = -1;
    private InventorySlotUI[] m_EquipSlots;
    private ChessSlotContainerImpl m_EquipContainer;
    private InventorySlot[] m_EquipSlotData; // 装备槽数据包装

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        m_RectTransform = GetComponent<RectTransform>();
        if (m_RectTransform == null)
        {
            DebugEx.ErrorModule("DetailInfoUI", "未找到 RectTransform 组件");
        }

        InitEquipSlots();
    }

    private void OnEnable()
    {
        ChessStateEvents.OnEquipmentChanged += OnEquipmentChangedHandler;
    }

    private void OnDisable()
    {
        ChessStateEvents.OnEquipmentChanged -= OnEquipmentChangedHandler;
        m_SlideInTween?.Kill();
    }

    #endregion

    #region 数据设置

    /// <summary>
    /// 设置卡牌数据
    /// </summary>
    public void SetData(CardData cardData)
    {
        m_CardData = cardData;
        m_ChessEntity = null;
        m_CurrentMode = 0;
        m_CurrentChessId = -1;
        DebugEx.LogModule("DetailInfoUI", $"设置卡牌数据: {cardData?.Name ?? "null"}");
    }

    /// <summary>
    /// 设置棋子数据（战斗阶段）
    /// </summary>
    public void SetChessUnitData(ChessEntity chessEntity)
    {
        m_ChessEntity = chessEntity;
        m_CardData = null;
        m_ChessConfig = null;
        m_GlobalState = null;
        m_CurrentMode = 1;
        m_CurrentChessId = chessEntity?.ChessId ?? -1;
        UpdateEquipContainerChessId();
        DebugEx.LogModule("DetailInfoUI", $"设置棋子数据: {chessEntity?.Config?.Name ?? "null"}");
    }

    /// <summary>
    /// 设置棋子配置数据（准备阶段）
    /// </summary>
    public void SetChessConfig(SummonChessConfig config, GlobalChessState globalState)
    {
        m_ChessConfig = config;
        m_GlobalState = globalState;
        m_ChessEntity = null;
        m_CardData = null;
        m_CurrentMode = 2;
        m_CurrentChessId = config?.Id ?? -1;
        UpdateEquipContainerChessId();
        DebugEx.LogModule("DetailInfoUI", $"设置棋子配置: {config?.Name ?? "null"}");
    }

    /// <summary>
    /// ⭐ 新增：在准备阶段关联 ChessEntity（用于显示实时属性）
    /// </summary>
    public void SetChessEntityForPreparation(ChessEntity entity)
    {
        if (entity != null)
        {
            m_ChessEntity = entity;
            DebugEx.LogModule("DetailInfoUI", $"已关联ChessEntity用于准备阶段: {entity.Config?.Name ?? "null"}");
        }
    }

    #endregion

    #region UI 刷新

    /// <summary>
    /// 刷新UI显示（自动判断模式）
    /// </summary>
    public void RefreshUI()
    {
        if (m_CurrentMode == 0)
        {
            RefreshCardUI();
        }
        else if (m_CurrentMode == 1)
        {
            RefreshChessUnitUI();
        }
        else if (m_CurrentMode == 2)
        {
            RefreshChessConfigUI();
        }
    }

    /// <summary>
    /// 刷新卡牌UI显示
    /// </summary>
    private void RefreshCardUI()
    {
        if (m_CardData == null)
        {
            DebugEx.WarningModule("DetailInfoUI", "卡牌数据为空，无法刷新UI");
            return;
        }

        if (varBuffBg != null) varBuffBg.gameObject.SetActive(false);
        if (varEquipBg != null) varEquipBg.gameObject.SetActive(false);

        if (varTitleText != null)
            varTitleText.text = m_CardData.Name;

        if (varDesc_1Text != null)
            varDesc_1Text.text = $"灵力消耗: {m_CardData.SpiritCost}  范围: {m_CardData.AreaRadius}";

        if (varDesc_2Text != null)
            varDesc_2Text.text = m_CardData.Desc;

        DebugEx.LogModule("DetailInfoUI", $"卡牌UI已刷新: {m_CardData.Name}");
    }

    /// <summary>
    /// 刷新棋子UI显示（战斗阶段，包含Buff）
    /// </summary>
    private void RefreshChessUnitUI()
    {
        if (m_ChessEntity == null || m_ChessEntity.Config == null)
        {
            DebugEx.WarningModule("DetailInfoUI", "棋子数据为空，无法刷新UI");
            return;
        }

        if (varBuffBg != null) varBuffBg.gameObject.SetActive(true);
        if (varEquipBg != null) varEquipBg.gameObject.SetActive(true);

        var config = m_ChessEntity.Config;
        var attr = m_ChessEntity.Attribute;

        if (varTitleText != null)
            varTitleText.text = $"{config.Name} {new string('★', config.StarLevel)}";

        if (varDesc_1Text != null)
        {
            varDesc_1Text.text = $"HP: {attr.CurrentHp:F0}/{attr.MaxHp:F0}\n"
                               + $"MP: {attr.CurrentMp:F0}/{config.MaxMp:F0}\n"
                               + $"攻击: {attr.AtkDamage:F0}  护甲: {attr.Armor:F0}\n"
                               + $"魔抗: {attr.MagicResist:F0}  速度: {config.MoveSpeed:F1}\n"
                               + $"暴击率: {config.CritRate * 100:F0}%  人口: {config.PopCost}";
        }

        if (varDesc_2Text != null)
            varDesc_2Text.text = config.Description;

        RefreshAllBuffs();
        RefreshEquipmentUI();

        DebugEx.LogModule("DetailInfoUI", $"棋子UI已刷新: {config.Name}");
    }

    /// <summary>
    /// 刷新棋子配置UI显示（准备阶段）
    /// ⭐ 修改：支持显示 ChessEntity 的实时属性（如果有关联的实体）
    /// </summary>
    private void RefreshChessConfigUI()
    {
        if (m_ChessConfig == null)
        {
            DebugEx.WarningModule("DetailInfoUI", "棋子配置为空，无法刷新UI");
            return;
        }

        if (varBuffBg != null) varBuffBg.gameObject.SetActive(false);
        if (varEquipBg != null) varEquipBg.gameObject.SetActive(true);

        var config = m_ChessConfig;

        if (varTitleText != null)
            varTitleText.text = $"{config.Name} {new string('★', config.StarLevel)}";

        if (varDesc_1Text != null)
        {
            // ⭐ 优先使用 ChessEntity 的实时属性，如果没有则使用配置和全局状态
            if (m_ChessEntity != null && m_ChessEntity.Attribute != null)
            {
                // 使用 ChessEntity 的实时属性（HP、MP、攻击等）
                var attr = m_ChessEntity.Attribute;
                var globalState = GlobalChessManager.Instance?.GetChessState(config.Id) ?? m_GlobalState;
                int level = globalState?.Level ?? 1;
                int experience = globalState?.Experience ?? 0;

                varDesc_1Text.text = $"等级: {level}  经验: {experience}\n"
                                   + $"HP: {attr.CurrentHp:F0}/{attr.MaxHp:F0}\n"
                                   + $"MP: {attr.CurrentMp:F0}/{config.MaxMp:F0}\n"
                                   + $"攻击: {attr.AtkDamage:F0}  护甲: {attr.Armor:F0}\n"
                                   + $"魔抗: {attr.MagicResist:F0}  速度: {config.MoveSpeed:F1}\n"
                                   + $"暴击率: {config.CritRate * 100:F0}%  人口: {config.PopCost}";

                DebugEx.LogModule("DetailInfoUI", $"棋子配置UI已刷新（使用实体属性）: {config.Name} HP={attr.CurrentHp:F0}/{attr.MaxHp:F0}");
            }
            else if (m_GlobalState != null)
            {
                // 使用全局状态（静态数据）
                var state = m_GlobalState;
                varDesc_1Text.text = $"等级: {state.Level}  经验: {state.Experience}\n"
                                   + $"HP: {state.CurrentHp:F0}/{state.MaxHp:F0}\n"
                                   + $"MP: {config.InitialMp:F0}/{config.MaxMp:F0}\n"
                                   + $"攻击: {config.AtkDamage:F0}  护甲: {config.Armor:F0}\n"
                                   + $"魔抗: {config.MagicResist:F0}  速度: {config.MoveSpeed:F1}\n"
                                   + $"暴击率: {config.CritRate * 100:F0}%  人口: {config.PopCost}";

                DebugEx.LogModule("DetailInfoUI", $"棋子配置UI已刷新（使用全局状态）: {config.Name}");
            }
        }

        if (varDesc_2Text != null)
            varDesc_2Text.text = config.Description;

        RefreshEquipmentUI();
    }

    #endregion

    #region Buff管理

    /// <summary>
    /// 刷新所有Buff显示
    /// </summary>
    private void RefreshAllBuffs()
    {
        ClearAllBuffItems();

        if (m_ChessEntity == null || m_ChessEntity.BuffManager == null) return;

        var allBuffs = m_ChessEntity.BuffManager.GetAllBuffs();
        foreach (var buff in allBuffs)
        {
            AddBuffItem(buff.BuffId, buff.StackCount);
        }
    }

    /// <summary>
    /// 添加单个BuffItem
    /// </summary>
    private void AddBuffItem(int buffId, int stackCount)
    {
        if (varBuffBg == null || varBuffItem == null) return;

        // 检查是否已存在
        if (m_BuffItems.ContainsKey(buffId))
        {
            return;
        }

        // 实例化BuffItem
        GameObject buffItemGo = Instantiate(varBuffItem, varBuffBg.transform, false);
        BuffItem buffItem = buffItemGo.GetComponent<BuffItem>();

        if (buffItem != null)
        {
            buffItem.SetData(buffId);
            buffItem.SetStackCount(stackCount);
            m_BuffItems[buffId] = buffItem;
            buffItemGo.SetActive(true);
        }
    }

    /// <summary>
    /// 清除所有BuffItem
    /// </summary>
    private void ClearAllBuffItems()
    {
        foreach (var buffItem in m_BuffItems.Values)
        {
            if (buffItem != null && buffItem.gameObject != null)
            {
                Destroy(buffItem.gameObject);
            }
        }
        m_BuffItems.Clear();
    }

    #endregion

    #region 动画

    /// <summary>
    /// 显示 DetailInfoUI 并播放滑入动画
    /// </summary>
    public void ShowWithAnimation()
    {
        if (m_RectTransform == null)
        {
            DebugEx.ErrorModule("DetailInfoUI", "RectTransform 为空，无法播放动画");
            gameObject.SetActive(true);
            return;
        }

        // 杀死之前的动画
        m_SlideInTween?.Kill();

        // 设置初始位置（x = 360）
        var anchoredPos = m_RectTransform.anchoredPosition;
        anchoredPos.x = INITIAL_X;
        m_RectTransform.anchoredPosition = anchoredPos;

        // 激活 GameObject
        gameObject.SetActive(true);

        // 播放滑入动画（x: 360 → 0）
        m_SlideInTween = m_RectTransform.DOAnchorPosX(TARGET_X, SLIDE_IN_DURATION)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                DebugEx.LogModule("DetailInfoUI", "滑入动画完成");
            });

        DebugEx.LogModule("DetailInfoUI", "开始播放滑入动画");
    }

    #endregion

    #region 装备管理

    /// <summary>
    /// 初始化装备槽UI
    /// </summary>
    private void InitEquipSlots()
    {
        if (varEquipBg == null || varInventorySlotUI == null)
        {
            DebugEx.WarningModule("DetailInfoUI", "装备槽模板或容器为空，跳过初始化");
            return;
        }

        // 隐藏模板
        varInventorySlotUI.SetActive(false);

        // 创建装备容器组件
        m_EquipContainer = varEquipBg.gameObject.GetComponent<ChessSlotContainerImpl>();
        if (m_EquipContainer == null)
        {
            m_EquipContainer = varEquipBg.gameObject.AddComponent<ChessSlotContainerImpl>();
        }

        // 初始化装备槽数据
        m_EquipSlotData = new InventorySlot[ChessEquipmentManager.EQUIP_SLOT_COUNT];
        for (int i = 0; i < ChessEquipmentManager.EQUIP_SLOT_COUNT; i++)
        {
            m_EquipSlotData[i] = new InventorySlot(i);
        }
        m_EquipContainer.SetEquipSlotData(m_EquipSlotData);
        m_EquipContainer.SetDetailInfoUI(this);

        // 实例化装备槽UI
        m_EquipSlots = new InventorySlotUI[ChessEquipmentManager.EQUIP_SLOT_COUNT];
        for (int i = 0; i < ChessEquipmentManager.EQUIP_SLOT_COUNT; i++)
        {
            var slotGo = Instantiate(varInventorySlotUI, varEquipBg.transform, false);
            slotGo.SetActive(true);

            var slotUI = slotGo.GetComponent<InventorySlotUI>();
            if (slotUI != null)
            {
                slotUI.SetSlotIndex(i);
                slotUI.SetContainerType(SlotContainerType.Chess);
                slotUI.SetSlotContainer(m_EquipContainer);
                m_EquipSlots[i] = slotUI;
            }
        }

        DebugEx.LogModule("DetailInfoUI", $"装备槽初始化完成，共 {ChessEquipmentManager.EQUIP_SLOT_COUNT} 个槽位");
    }

    /// <summary>
    /// 刷新装备UI显示
    /// </summary>
    private void RefreshEquipmentUI()
    {
        if (m_EquipSlots == null || m_CurrentChessId < 0)
            return;

        var equipMgr = ChessEquipmentManager.Instance;

        for (int i = 0; i < m_EquipSlots.Length; i++)
        {
            if (m_EquipSlots[i] == null)
                continue;

            var equipItem = equipMgr.GetEquippedItem(m_CurrentChessId, i);

            if (equipItem != null)
            {
                // 更新数据包装
                m_EquipSlotData[i].SetItem(equipItem, 1);
                m_EquipSlots[i].SetData(m_EquipSlotData[i].ItemStack);
            }
            else
            {
                m_EquipSlotData[i].Clear();
                m_EquipSlots[i].SetData(null);
            }
        }
    }

    /// <summary>
    /// 更新装备容器的棋子ID
    /// </summary>
    private void UpdateEquipContainerChessId()
    {
        if (m_EquipContainer != null)
        {
            m_EquipContainer.SetChessId(m_CurrentChessId);
        }
    }

    /// <summary>
    /// 装备变更事件处理
    /// </summary>
    private void OnEquipmentChangedHandler(int chessId, int slotIndex)
    {
        if (chessId != m_CurrentChessId)
            return;

        RefreshEquipmentUI();

        // 装备变更也需要刷新属性显示
        if (m_CurrentMode == 1)
        {
            RefreshChessUnitUI();
        }
        else if (m_CurrentMode == 2)
        {
            RefreshChessConfigUI();
        }
    }

    /// <summary>
    /// 右键卸下装备（由装备槽的右键事件调用）
    /// </summary>
    public void UnequipFromSlot(int slotIndex)
    {
        if (m_CurrentChessId < 0)
            return;

        var equipMgr = ChessEquipmentManager.Instance;
        var item = equipMgr.UnequipItem(m_CurrentChessId, slotIndex);
        if (item != null)
        {
            // 装备回到背包
            bool added = InventoryManager.Instance.AddItem(item.ItemId, 1);
            if (!added)
            {
                // 背包满了，重新穿上
                equipMgr.EquipItem(m_CurrentChessId, item, slotIndex);
                DebugEx.WarningModule("DetailInfoUI", "背包已满，无法卸下装备");
            }
            else
            {
                DebugEx.LogModule("DetailInfoUI", $"卸下装备 {item.Name} → 背包");
            }
        }
    }

    #endregion
}

