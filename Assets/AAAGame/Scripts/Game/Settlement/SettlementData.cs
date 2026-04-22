using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 结算数据容器
/// 存储本局游戏的结算统计数据（经验、金币、掉落物品等）
/// </summary>
public class SettlementData
{
    /// <summary>目标场景ID</summary>
    public string TargetScene { get; set; }

    /// <summary>结算触发源</summary>
    public SettlementTriggerSource TriggerSource { get; set; }

    /// <summary>获得的经验值</summary>
    public int Experience { get; set; }

    /// <summary>获得的金币</summary>
    public int Currency { get; set; }

    /// <summary>掉落物品列表</summary>
    public List<int> DroppedItems { get; set; } = new List<int>();

    /// <summary>击杀敌人数量</summary>
    public int EnemiesDefeated { get; set; }

    /// <summary>本局游戏时长（秒）</summary>
    public float SessionDuration { get; set; }

    /// <summary>是否为失败场景（true=死亡失败，false=传送胜利）</summary>
    public bool IsDefeat { get; set; }

    /// <summary>获取总经验值</summary>
    public int GetTotalExperience()
    {
        return Experience;
    }

    /// <summary>获取总金币</summary>
    public int GetTotalCurrency()
    {
        return Currency;
    }

    /// <summary>获取掉落物品列表</summary>
    public List<int> GetItemList()
    {
        return new List<int>(DroppedItems);
    }

    /// <summary>是否为失败场景</summary>
    public bool IsDefeatScenario()
    {
        return IsDefeat;
    }
}
