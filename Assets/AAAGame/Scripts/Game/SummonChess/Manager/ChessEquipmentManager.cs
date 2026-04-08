using System;
using System.Collections.Generic;

/// <summary>
/// 棋子装备管理器（单例）
/// 管理所有棋子的装备穿戴数据
///
/// 使用场景：
/// - 穿戴装备：从背包移除物品，加到棋子指定槽位
/// - 卸下装备：从棋子槽位移除，回到背包
/// - 查询装备：获取棋子当前装备列表
/// - 属性应用：穿戴/卸下时修改 ChessAttribute
/// </summary>
public class ChessEquipmentManager
{
    #region 常量

    /// <summary>每个棋子的装备槽数量</summary>
    public const int EQUIP_SLOT_COUNT = 3;

    #endregion

    #region 单例

    private static ChessEquipmentManager s_Instance;

    public static ChessEquipmentManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new ChessEquipmentManager();
            }
            return s_Instance;
        }
    }

    private ChessEquipmentManager()
    {
        m_ChessEquipments = new Dictionary<int, EquipmentItem[]>();
    }

    #endregion

    #region 私有字段

    /// <summary>棋子装备数据（ChessId → EquipmentItem[3]）</summary>
    private readonly Dictionary<int, EquipmentItem[]> m_ChessEquipments;

    #endregion

    #region 装备操作

    /// <summary>
    /// 穿戴装备
    /// </summary>
    /// <param name="chessId">棋子ID</param>
    /// <param name="item">装备物品</param>
    /// <param name="slotIndex">槽位索引（0~2）</param>
    /// <returns>被替换的旧装备（无则返回null）</returns>
    public EquipmentItem EquipItem(int chessId, EquipmentItem item, int slotIndex)
    {
        if (item == null)
        {
            DebugEx.WarningModule("ChessEquipMgr", "EquipItem: item is null");
            return null;
        }

        if (slotIndex < 0 || slotIndex >= EQUIP_SLOT_COUNT)
        {
            DebugEx.WarningModule("ChessEquipMgr", $"EquipItem: slotIndex {slotIndex} 越界");
            return null;
        }

        // 确保棋子有装备数组
        if (!m_ChessEquipments.TryGetValue(chessId, out var slots))
        {
            slots = new EquipmentItem[EQUIP_SLOT_COUNT];
            m_ChessEquipments[chessId] = slots;
        }

        // 取出旧装备
        EquipmentItem oldItem = slots[slotIndex];
        if (oldItem != null)
        {
            oldItem.IsEquipped = false;
            RemoveEquipmentStats(chessId, oldItem);
            DebugEx.LogModule("ChessEquipMgr", $"棋子 {chessId} 槽位 {slotIndex} 卸下旧装备: {oldItem.Name}");
        }

        // 穿戴新装备
        slots[slotIndex] = item;
        item.IsEquipped = true;
        ApplyEquipmentStats(chessId, item);

        DebugEx.LogModule("ChessEquipMgr", $"棋子 {chessId} 槽位 {slotIndex} 穿戴装备: {item.Name}");

        // 触发事件
        ChessStateEvents.FireEquipmentChanged(chessId, slotIndex);

        return oldItem;
    }

    /// <summary>
    /// 卸下装备
    /// </summary>
    /// <param name="chessId">棋子ID</param>
    /// <param name="slotIndex">槽位索引（0~2）</param>
    /// <returns>被卸下的装备（无则返回null）</returns>
    public EquipmentItem UnequipItem(int chessId, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= EQUIP_SLOT_COUNT)
        {
            DebugEx.WarningModule("ChessEquipMgr", $"UnequipItem: slotIndex {slotIndex} 越界");
            return null;
        }

        if (!m_ChessEquipments.TryGetValue(chessId, out var slots))
        {
            return null;
        }

        EquipmentItem item = slots[slotIndex];
        if (item == null)
        {
            DebugEx.LogModule("ChessEquipMgr", $"棋子 {chessId} 槽位 {slotIndex} 无装备");
            return null;
        }

        // 移除装备
        slots[slotIndex] = null;
        item.IsEquipped = false;
        RemoveEquipmentStats(chessId, item);

        DebugEx.LogModule("ChessEquipMgr", $"棋子 {chessId} 槽位 {slotIndex} 卸下装备: {item.Name}");

        // 触发事件
        ChessStateEvents.FireEquipmentChanged(chessId, slotIndex);

        return item;
    }

    #endregion

    #region 查询

    /// <summary>
    /// 获取棋子指定槽位的装备
    /// </summary>
    public EquipmentItem GetEquippedItem(int chessId, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= EQUIP_SLOT_COUNT)
            return null;

        if (!m_ChessEquipments.TryGetValue(chessId, out var slots))
            return null;

        return slots[slotIndex];
    }

    /// <summary>
    /// 获取棋子所有装备槽
    /// </summary>
    public EquipmentItem[] GetEquippedItems(int chessId)
    {
        if (!m_ChessEquipments.TryGetValue(chessId, out var slots))
            return null;

        return slots;
    }

    /// <summary>
    /// 指定槽位是否有装备
    /// </summary>
    public bool HasEquipment(int chessId, int slotIndex)
    {
        return GetEquippedItem(chessId, slotIndex) != null;
    }

    /// <summary>
    /// 获取棋子第一个空槽位索引（-1表示没有空槽）
    /// </summary>
    public int GetFirstEmptySlot(int chessId)
    {
        if (!m_ChessEquipments.TryGetValue(chessId, out var slots))
            return 0; // 未注册过，第一个槽肯定是空的

        for (int i = 0; i < EQUIP_SLOT_COUNT; i++)
        {
            if (slots[i] == null)
                return i;
        }

        return -1; // 满了
    }

    #endregion

    #region 属性应用

    /// <summary>
    /// 应用装备属性加成到棋子
    /// </summary>
    private void ApplyEquipmentStats(int chessId, EquipmentItem item)
    {
        var entity = FindChessEntity(chessId);
        if (entity == null || entity.Attribute == null || item.BaseAttributes == null)
            return;

        foreach (var attr in item.BaseAttributes)
        {
            ApplyAttributeModifier(entity.Attribute, attr.Key, attr.Value);
        }

        DebugEx.LogModule("ChessEquipMgr", $"已应用装备属性: {item.Name} → 棋子 {chessId}");
    }

    /// <summary>
    /// 移除装备属性加成
    /// </summary>
    private void RemoveEquipmentStats(int chessId, EquipmentItem item)
    {
        var entity = FindChessEntity(chessId);
        if (entity == null || entity.Attribute == null || item.BaseAttributes == null)
            return;

        foreach (var attr in item.BaseAttributes)
        {
            ApplyAttributeModifier(entity.Attribute, attr.Key, -attr.Value);
        }

        DebugEx.LogModule("ChessEquipMgr", $"已移除装备属性: {item.Name} ← 棋子 {chessId}");
    }

    /// <summary>
    /// 重新应用棋子所有装备属性（实体初始化后调用）
    /// </summary>
    public void ReapplyAllEquipmentStats(int chessId)
    {
        if (!m_ChessEquipments.TryGetValue(chessId, out var slots))
            return;

        for (int i = 0; i < EQUIP_SLOT_COUNT; i++)
        {
            if (slots[i] != null)
            {
                ApplyEquipmentStats(chessId, slots[i]);
            }
        }
    }

    /// <summary>
    /// 对属性应用修改值
    /// </summary>
    private void ApplyAttributeModifier(ChessAttribute attribute, AttributeType type, float value)
    {
        switch (type)
        {
            case AttributeType.Attack:
                attribute.ModifyAtkDamage(value);
                break;
            case AttributeType.MaxHP:
                attribute.SetMaxHp(attribute.MaxHp + value);
                break;
            case AttributeType.CritRate:
                attribute.ModifyCritRate(value);
                break;
            case AttributeType.AttackSpeed:
                attribute.ModifyAtkSpeed(value);
                break;
            case AttributeType.MoveSpeed:
                attribute.ModifyMoveSpeed(value);
                break;
            case AttributeType.Defense:
                attribute.ModifyArmor(value);
                break;
            case AttributeType.MagicPower:
                attribute.ModifySpellPower(value);
                break;
            default:
                DebugEx.WarningModule("ChessEquipMgr", $"未处理的 AttributeType: {type}");
                break;
        }
    }

    /// <summary>
    /// 查找棋子实体（优先从 CombatEntityTracker，否则从 SummonChessManager）
    /// </summary>
    private ChessEntity FindChessEntity(int chessId)
    {
        // 优先从战斗追踪器查找
        if (CombatEntityTracker.Instance != null)
        {
            var allChess = CombatEntityTracker.Instance.GetAllAliveChess();
            foreach (var entity in allChess)
            {
                if (entity != null && entity.ChessId == chessId)
                    return entity;
            }
        }

        // 从运行时管理器查找
        if (SummonChessManager.Instance != null)
        {
            var allChess = SummonChessManager.Instance.GetAllChess();
            foreach (var entity in allChess)
            {
                if (entity != null && entity.ChessId == chessId)
                    return entity;
            }
        }

        return null;
    }

    #endregion

    #region 存档

    /// <summary>
    /// 保存装备数据
    /// </summary>
    public List<ChessEquipmentSaveData> SaveEquipments()
    {
        var saveList = new List<ChessEquipmentSaveData>();

        foreach (var kvp in m_ChessEquipments)
        {
            int chessId = kvp.Key;
            var slots = kvp.Value;

            for (int i = 0; i < EQUIP_SLOT_COUNT; i++)
            {
                if (slots[i] != null)
                {
                    saveList.Add(new ChessEquipmentSaveData
                    {
                        ChessId = chessId,
                        SlotIndex = i,
                        ItemId = slots[i].ItemId
                    });
                }
            }
        }

        DebugEx.LogModule("ChessEquipMgr", $"装备数据保存完成，共 {saveList.Count} 件装备");
        return saveList;
    }

    /// <summary>
    /// 加载装备数据
    /// </summary>
    public void LoadEquipments(List<ChessEquipmentSaveData> saveList)
    {
        m_ChessEquipments.Clear();

        if (saveList == null || saveList.Count == 0)
        {
            DebugEx.LogModule("ChessEquipMgr", "无装备存档数据");
            return;
        }

        foreach (var data in saveList)
        {
            var item = ItemManager.Instance?.CreateItem(data.ItemId) as EquipmentItem;
            if (item == null)
            {
                DebugEx.WarningModule("ChessEquipMgr", $"加载装备失败 ItemId={data.ItemId}");
                continue;
            }

            if (!m_ChessEquipments.TryGetValue(data.ChessId, out var slots))
            {
                slots = new EquipmentItem[EQUIP_SLOT_COUNT];
                m_ChessEquipments[data.ChessId] = slots;
            }

            if (data.SlotIndex >= 0 && data.SlotIndex < EQUIP_SLOT_COUNT)
            {
                slots[data.SlotIndex] = item;
                item.IsEquipped = true;
            }
        }

        DebugEx.LogModule("ChessEquipMgr", $"装备数据加载完成，共 {saveList.Count} 件");
    }

    #endregion

    #region 清理

    /// <summary>
    /// 注销棋子的所有装备（棋子从阵容移除时调用）
    /// </summary>
    public void UnregisterChess(int chessId)
    {
        if (m_ChessEquipments.Remove(chessId))
        {
            DebugEx.LogModule("ChessEquipMgr", $"注销棋子 {chessId} 的所有装备");
        }
    }

    /// <summary>
    /// 清空所有数据
    /// </summary>
    public void Clear()
    {
        m_ChessEquipments.Clear();
        DebugEx.LogModule("ChessEquipMgr", "所有装备数据已清空");
    }

    #endregion
}

/// <summary>
/// 装备存档数据
/// </summary>
[System.Serializable]
public struct ChessEquipmentSaveData
{
    public int ChessId;
    public int SlotIndex;
    public int ItemId;
}
