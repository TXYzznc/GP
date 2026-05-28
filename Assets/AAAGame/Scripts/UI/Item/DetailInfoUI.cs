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

    private const float SLIDE_DURATION = 0.35f;
    private const float OFFSCREEN_X = 360f;
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

    // 装备/宝物加成数据结构（覆盖全部 AttributeType）
    private struct StatBonus
    {
        public double MaxHp;
        public double MaxMp;
        public double AtkDamage;
        public double Armor;
        public double MagicResist;
        public double AtkSpeed;
        public double CritRate;
        public double CritDamage;
        public double SpellPower;
        public double MoveSpeed;
        public double CooldownReduce;

        public void Add(AttributeType type, double value)
        {
            switch (type)
            {
                case AttributeType.MaxHP:         MaxHp += value; break;
                case AttributeType.MaxMP:         MaxMp += value; break;
                case AttributeType.Attack:        AtkDamage += value; break;
                case AttributeType.Defense:       Armor += value; break;
                case AttributeType.MagicResist:   MagicResist += value; break;
                case AttributeType.AttackSpeed:   AtkSpeed += value; break;
                case AttributeType.CritRate:      CritRate += value; break;
                case AttributeType.CritDamage:    CritDamage += value; break;
                case AttributeType.SpellPower:    SpellPower += value; break;
                case AttributeType.MoveSpeed:     MoveSpeed += value; break;
                case AttributeType.CooldownReduce: CooldownReduce += value; break;
            }
        }
    }

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        m_RectTransform = GetComponent<RectTransform>();
        if (m_RectTransform == null)
        {
            DebugEx.Error(nameof(DetailInfoUI), "未找到 RectTransform 组件");
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
        DebugEx.Log(nameof(DetailInfoUI), $"设置卡牌数据: {cardData?.Name ?? "null"}");
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
        DebugEx.Log(nameof(DetailInfoUI), $"设置棋子数据: {chessEntity?.Config?.Name ?? "null"}");
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
        DebugEx.Log(nameof(DetailInfoUI), $"设置棋子配置: {config?.Name ?? "null"}");
    }

    /// <summary>
    /// ⭐ 新增：在准备阶段关联 ChessEntity（用于显示实时属性）
    /// </summary>
    public void SetChessEntityForPreparation(ChessEntity entity)
    {
        if (entity != null)
        {
            m_ChessEntity = entity;
            DebugEx.Log(nameof(DetailInfoUI), $"已关联ChessEntity用于准备阶段: {entity.Config?.Name ?? "null"}");
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
            DebugEx.Warning(nameof(DetailInfoUI), "卡牌数据为空，无法刷新UI");
            return;
        }

        if (varBuffBg != null) varBuffBg.gameObject.SetActive(false);
        if (varEquipBg != null) varEquipBg.gameObject.SetActive(false);
        if (varDesc_1Text != null) varDesc_1Text.gameObject.SetActive(true);
        if (varChessAttribute != null) varChessAttribute.gameObject.SetActive(false);
        if (varPopulation != null) varPopulation.gameObject.SetActive(false);

        if (varTitleText != null)
            varTitleText.text = m_CardData.Name;

        if (varDesc_1Text != null)
            varDesc_1Text.text = $"灵力消耗: {m_CardData.SpiritCost}  范围: {m_CardData.AreaRadius}";

        if (varDesc_2Text != null)
            varDesc_2Text.text = m_CardData.Desc;

        DebugEx.Log(nameof(DetailInfoUI), $"卡牌UI已刷新: {m_CardData.Name}");
    }

    /// <summary>
    /// 刷新棋子UI显示（战斗阶段，包含Buff）
    /// </summary>
    private void RefreshChessUnitUI()
    {
        if (m_ChessEntity == null || m_ChessEntity.Config == null)
        {
            DebugEx.Warning(nameof(DetailInfoUI), "棋子数据为空，无法刷新UI");
            return;
        }

        if (varBuffBg != null) varBuffBg.gameObject.SetActive(true);
        if (varEquipBg != null) varEquipBg.gameObject.SetActive(true);
        if (varDesc_1Text != null) varDesc_1Text.gameObject.SetActive(false);
        if (varChessAttribute != null) varChessAttribute.gameObject.SetActive(true);
        if (varPopulation != null) varPopulation.gameObject.SetActive(true);

        var config = m_ChessEntity.Config;
        var attr = m_ChessEntity.Attribute;

        if (varTitleText != null)
            varTitleText.text = $"{config.Name} Lv{m_ChessEntity.Rank}";

        if (varDesc_2Text != null)
            varDesc_2Text.text = config.GetDescription(m_ChessEntity.Rank);

        // 刷新属性显示
        RefreshAttributeDisplay(attr, config, m_ChessEntity.Rank);

        // 刷新人口显示
        if (varPopulationText != null)
            varPopulationText.text = $"{config.PopCost}";

        RefreshAllBuffs();
        RefreshEquipmentUI();

        DebugEx.Log(nameof(DetailInfoUI), $"棋子UI已刷新: {config.Name}");
    }

    /// <summary>
    /// 刷新棋子配置UI显示（准备阶段）
    /// </summary>
    private void RefreshChessConfigUI()
    {
        if (m_ChessConfig == null)
        {
            DebugEx.Warning(nameof(DetailInfoUI), "棋子配置为空，无法刷新UI");
            return;
        }

        if (varBuffBg != null) varBuffBg.gameObject.SetActive(false);
        if (varEquipBg != null) varEquipBg.gameObject.SetActive(true);
        if (varDesc_1Text != null) varDesc_1Text.gameObject.SetActive(false);
        if (varChessAttribute != null) varChessAttribute.gameObject.SetActive(true);
        if (varPopulation != null) varPopulation.gameObject.SetActive(true);

        var config = m_ChessConfig;

        if (varTitleText != null)
            varTitleText.text = $"{config.Name}";

        // 优先使用 ChessEntity 的实时属性，如果没有则使用配置和全局状态
        if (m_ChessEntity != null && m_ChessEntity.Attribute != null)
        {
            var attr = m_ChessEntity.Attribute;
            RefreshAttributeDisplay(attr, config, m_ChessEntity.Rank);
        }
        else if (m_GlobalState != null)
        {
            // 使用全局状态（静态数据）
            RefreshAttributeDisplayFromGlobalState(m_GlobalState, config);
        }

        if (varDesc_2Text != null)
            varDesc_2Text.text = config.GetDescription(1);

        // 刷新人口显示
        if (varPopulationText != null)
            varPopulationText.text = $"{config.PopCost}";

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
    /// 显示 DetailInfoUI 并播放滑入动画（从右到左）
    /// </summary>
    public void ShowWithAnimation()
    {
        if (m_RectTransform == null)
        {
            DebugEx.Error(nameof(DetailInfoUI), "RectTransform 为空，无法播放动画");
            gameObject.SetActive(true);
            return;
        }

        m_SlideInTween?.Kill();

        // 设置初始位置（屏幕右侧外）
        var anchoredPos = m_RectTransform.anchoredPosition;
        anchoredPos.x = OFFSCREEN_X;
        m_RectTransform.anchoredPosition = anchoredPos;

        gameObject.SetActive(true);

        // 从右滑入（x: 360 → 0）
        m_SlideInTween = m_RectTransform.DOAnchorPosX(TARGET_X, SLIDE_DURATION)
            .SetEase(Ease.OutCubic);
    }

    /// <summary>
    /// 隐藏 DetailInfoUI 并播放滑出动画（从左到右）
    /// </summary>
    public void HideWithAnimation()
    {
        if (m_RectTransform == null || !gameObject.activeSelf)
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
            return;
        }

        m_SlideInTween?.Kill();

        // 滑出到右侧（x → 360）
        m_SlideInTween = m_RectTransform.DOAnchorPosX(OFFSCREEN_X, SLIDE_DURATION)
            .SetEase(Ease.InCubic)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
    }

    #endregion

    #region 属性显示

    /// <summary>
    /// 刷新属性显示（含装备绿色加成 + 宝物蓝色加成）
    /// </summary>
    private void RefreshAttributeDisplay(ChessAttribute attr, SummonChessConfig config, int rank)
    {
        if (attr == null || config == null) return;

        var globalState = GlobalChessManager.Instance?.GetChessState(config.Id) ?? m_GlobalState;
        int level = globalState?.Level ?? rank;
        int experience = globalState?.Experience ?? 0;

        var equipBonus   = GetEquipmentBonus(m_CurrentChessId);
        var treasureBonus = GetTreasureBonus(m_CurrentChessId);

        if (varGradeText != null) varGradeText.text = $"{level}";
        if (varExpText != null)   varExpText.text   = $"{experience}";

        // HP
        if (varHPText != null)
        {
            double baseHp = attr.MaxHp - equipBonus.MaxHp - treasureBonus.MaxHp;
            string txt = $"{attr.CurrentHp:F0}/";
            txt += equipBonus.MaxHp > 0 || treasureBonus.MaxHp > 0
                ? $"<color=white>{baseHp:F0}</color>"
                : $"{attr.MaxHp:F0}";
            if (equipBonus.MaxHp > 0)   txt += $"<color=green>+{equipBonus.MaxHp:F0}</color>";
            if (treasureBonus.MaxHp > 0) txt += $"<color=#6BB5FF>+{treasureBonus.MaxHp:F0}</color>";
            varHPText.text = txt;
        }

        // MP
        if (varMpText != null)
        {
            double baseMp = config.GetMaxMp(rank);
            string txt = $"{attr.CurrentMp:F0}/";
            txt += equipBonus.MaxMp > 0 || treasureBonus.MaxMp > 0
                ? $"<color=white>{baseMp:F0}</color>"
                : $"{baseMp:F0}";
            if (equipBonus.MaxMp > 0)   txt += $"<color=green>+{equipBonus.MaxMp:F0}</color>";
            if (treasureBonus.MaxMp > 0) txt += $"<color=#6BB5FF>+{treasureBonus.MaxMp:F0}</color>";
            varMpText.text = txt;
        }

        // 攻击
        if (varAttackText != null)
        {
            double baseAtk = attr.AtkDamage - equipBonus.AtkDamage - treasureBonus.AtkDamage;
            string txt = equipBonus.AtkDamage > 0 || treasureBonus.AtkDamage > 0
                ? $"<color=white>{baseAtk:F0}</color>"
                : $"{attr.AtkDamage:F0}";
            if (equipBonus.AtkDamage > 0)   txt += $"<color=green>+{equipBonus.AtkDamage:F0}</color>";
            if (treasureBonus.AtkDamage > 0) txt += $"<color=#6BB5FF>+{treasureBonus.AtkDamage:F0}</color>";
            varAttackText.text = txt;
        }

        // 护甲
        if (varArmorText != null)
        {
            double baseArmor = attr.Armor - equipBonus.Armor - treasureBonus.Armor;
            string txt = equipBonus.Armor > 0 || treasureBonus.Armor > 0
                ? $"<color=white>{baseArmor:F0}</color>"
                : $"{attr.Armor:F0}";
            if (equipBonus.Armor > 0)   txt += $"<color=green>+{equipBonus.Armor:F0}</color>";
            if (treasureBonus.Armor > 0) txt += $"<color=#6BB5FF>+{treasureBonus.Armor:F0}</color>";
            varArmorText.text = txt;
        }

        // 魔抗
        if (varSpelResistanceText != null)
        {
            double baseMr = attr.MagicResist - equipBonus.MagicResist - treasureBonus.MagicResist;
            string txt = equipBonus.MagicResist > 0 || treasureBonus.MagicResist > 0
                ? $"<color=white>{baseMr:F0}</color>"
                : $"{attr.MagicResist:F0}";
            if (equipBonus.MagicResist > 0)   txt += $"<color=green>+{equipBonus.MagicResist:F0}</color>";
            if (treasureBonus.MagicResist > 0) txt += $"<color=#6BB5FF>+{treasureBonus.MagicResist:F0}</color>";
            varSpelResistanceText.text = txt;
        }

        // 攻速
        if (varAttackSpeedText != null && config.AtkSpeed != null && rank > 0 && rank <= config.AtkSpeed.Length)
        {
            double baseAs = config.AtkSpeed[rank - 1];
            string txt = treasureBonus.AtkSpeed > 0
                ? $"<color=white>{baseAs:F2}</color><color=#6BB5FF>+{treasureBonus.AtkSpeed:F2}</color>"
                : $"{attr.AtkSpeed:F2}";
            varAttackSpeedText.text = txt;
        }

        // 暴击率
        if (varCriticalChanceText != null && config.CritRate != null && rank > 0 && rank <= config.CritRate.Length)
        {
            double baseCr = config.CritRate[rank - 1] * 100;
            string txt = treasureBonus.CritRate > 0
                ? $"<color=white>{baseCr:F0}%</color><color=#6BB5FF>+{treasureBonus.CritRate * 100:F0}%</color>"
                : $"{baseCr:F0}%";
            varCriticalChanceText.text = txt;
        }

        // 暴击伤害
        if (varCriticalDamageText != null && config.CritDamage != null && rank > 0 && rank <= config.CritDamage.Length)
        {
            double baseCd = config.CritDamage[rank - 1];
            string txt = treasureBonus.CritDamage > 0
                ? $"<color=white>{baseCd:F0}</color><color=#6BB5FF>+{treasureBonus.CritDamage:F0}</color>"
                : $"{baseCd:F0}";
            varCriticalDamageText.text = txt;
        }

        // 法强
        if (varMagicalAttackText != null && config.SpellPower != null && rank > 0 && rank <= config.SpellPower.Length)
        {
            double baseSp = config.SpellPower[rank - 1];
            string txt = treasureBonus.SpellPower > 0
                ? $"<color=white>{baseSp:F0}</color><color=#6BB5FF>+{treasureBonus.SpellPower:F0}</color>"
                : $"{baseSp:F0}";
            varMagicalAttackText.text = txt;
        }

        // 移速
        if (varMoveSpeedText != null)
        {
            string txt = treasureBonus.MoveSpeed > 0
                ? $"<color=white>{config.MoveSpeed:F1}</color><color=#6BB5FF>+{treasureBonus.MoveSpeed:F1}</color>"
                : $"{config.MoveSpeed:F1}";
            varMoveSpeedText.text = txt;
        }
    }

    /// <summary>
    /// 统计装备（绿色）加成
    /// </summary>
    private StatBonus GetEquipmentBonus(int chessId)
    {
        var bonus = new StatBonus();
        if (chessId < 0) return bonus;

        var equipMgr = ChessEquipmentManager.Instance;
        if (equipMgr == null) return bonus;

        for (int i = 0; i < ChessEquipmentManager.EQUIP_SLOT_COUNT; i++)
        {
            var item = equipMgr.GetEquippedItem(chessId, i);
            if (item?.BaseAttributes == null) continue;
            foreach (var kv in item.BaseAttributes)
                bonus.Add(kv.Key, kv.Value);
        }
        return bonus;
    }

    /// <summary>
    /// 统计宝物（蓝色）加成（BaseAttributes + Affixes）
    /// </summary>
    private StatBonus GetTreasureBonus(int chessId)
    {
        var bonus = new StatBonus();
        if (chessId < 0) return bonus;

        var equippedTreasures = PlayerAccountDataManager.Instance?.GetChessEquipments(chessId);
        if (equippedTreasures == null) return bonus;

        foreach (var treasureInstance in equippedTreasures)
        {
            // BaseAttributes
            var staticData = ItemManager.Instance?.GetTreasureData(treasureInstance.TreasureId);
            if (staticData?.BaseAttributes != null)
            {
                foreach (var kv in staticData.BaseAttributes)
                    bonus.Add(kv.Key, kv.Value);
            }

            // Affixes（词条）
            if (treasureInstance.Affixes != null)
            {
                foreach (var affix in treasureInstance.Affixes)
                {
                    double val = affix.ValueMax;
                    if (affix.ValueType == ValueType.Percent) val /= 100.0;
                    bonus.Add(affix.AttributeType, val);
                }
            }
        }
        return bonus;
    }

    /// <summary>
    /// 从全局状态刷新属性显示（准备阶段使用）
    /// </summary>
    private void RefreshAttributeDisplayFromGlobalState(GlobalChessState state, SummonChessConfig config)
    {
        if (state == null || config == null) return;

        // 显示等级
        if (varGradeText != null)
            varGradeText.text = $"{state.Level}";

        // 显示经验值
        if (varExpText != null)
            varExpText.text = $"{state.Experience}";

        // 显示HP
        if (varHPText != null)
            varHPText.text = $"{state.CurrentHp:F0}/{state.MaxHp:F0}";

        // 显示MP
        if (varMpText != null)
            varMpText.text = $"0/{(float)config.GetMaxMp(1):F0}";

        // 显示攻击
        if (varAttackText != null)
            varAttackText.text = $"{(float)config.GetAtkDamage(1):F0}";

        // 显示护甲
        if (varArmorText != null)
            varArmorText.text = $"{(float)config.GetArmor(1):F0}";

        // 显示攻速
        if (varAttackSpeedText != null && config.AtkSpeed != null && config.AtkSpeed.Length > 0)
            varAttackSpeedText.text = $"{(float)config.AtkSpeed[0]:F1}";

        // 显示暴击率
        if (varCriticalChanceText != null && config.CritRate != null && config.CritRate.Length > 0)
            varCriticalChanceText.text = $"{(float)config.CritRate[0] * 100:F0}%";

        // 显示暴击伤害
        if (varCriticalDamageText != null && config.CritDamage != null && config.CritDamage.Length > 0)
            varCriticalDamageText.text = $"{(float)config.CritDamage[0]:F0}";

        // 显示法强
        if (varMagicalAttackText != null && config.SpellPower != null && config.SpellPower.Length > 0)
            varMagicalAttackText.text = $"{(float)config.SpellPower[0]:F0}";

        // 显示魔抗
        if (varSpelResistanceText != null)
            varSpelResistanceText.text = $"{(float)config.GetMagicResist(1):F0}";

        // 显示移动速度
        if (varMoveSpeedText != null)
            varMoveSpeedText.text = $"{(float)config.MoveSpeed:F1}";
    }

    #endregion

    #region 装备管理

    /// <summary>
    /// 初始化装备槽UI
    /// </summary>
    private void InitEquipSlots()
    {
        if (varEquipBg == null || varInventorySlotUI1Arr == null || varInventorySlotUI1Arr.Length == 0)
        {
            DebugEx.Warning(nameof(DetailInfoUI), "装备槽预制体或容器为空，跳过初始化");
            return;
        }

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

        // 使用预创建的装备槽UI
        m_EquipSlots = new InventorySlotUI[ChessEquipmentManager.EQUIP_SLOT_COUNT];
        for (int i = 0; i < ChessEquipmentManager.EQUIP_SLOT_COUNT && i < varInventorySlotUI1Arr.Length; i++)
        {
            var slotGo = varInventorySlotUI1Arr[i];
            if (slotGo == null)
                continue;

            var slotUI = slotGo.GetComponent<InventorySlotUI>();
            if (slotUI != null)
            {
                slotUI.SetSlotIndex(i);
                slotUI.SetContainerType(SlotContainerType.Chess);
                slotUI.SetSlotContainer(m_EquipContainer);
                m_EquipSlots[i] = slotUI;
            }
        }

        DebugEx.Log(nameof(DetailInfoUI), $"装备槽初始化完成，共 {ChessEquipmentManager.EQUIP_SLOT_COUNT} 个槽位");
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
                DebugEx.Warning(nameof(DetailInfoUI), "背包已满，无法卸下装备");
            }
            else
            {
                DebugEx.Log(nameof(DetailInfoUI), $"卸下装备 {item.Name} → 背包");
            }
        }
    }

    #endregion
}

