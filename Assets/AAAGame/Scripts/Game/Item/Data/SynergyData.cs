using System;
using System.Collections.Generic;

/// <summary>
/// 羁绊配置数据
/// </summary>
[Serializable]
public class SynergyData
{
    public int Id; // 羁绊ID
    public string Name; // 羁绊名称
    public SynergyType Type; // 羁绊类型（Chess=1 棋子羁绊，Treasure=2 宝物羁绊）
    public string Description; // 羁绊描述
    public int RequireCount; // 激活所需数量
    public List<int> RequireIds; // 需要的物品ID列表（仅用于宝物羁绊）
    public int EffectId; // 羁绊效果ID

    /// <summary>
    /// 检查是否满足激活条件（仅用于宝物羁绊）
    /// </summary>
    public bool CheckActivation(List<int> ownedIds)
    {
        if (RequireIds == null || RequireIds.Count == 0)
            return false;

        if (ownedIds == null || ownedIds.Count < RequireCount)
            return false;

        var ownedSet = new HashSet<int>(ownedIds);
        int matchCount = 0;
        foreach (int requireId in RequireIds)
        {
            if (ownedSet.Contains(requireId))
            {
                matchCount++;
                if (matchCount >= RequireCount)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 检查是否满足激活条件（调用方已有 HashSet 时使用，避免重复转换）
    /// </summary>
    public bool CheckActivation(HashSet<int> ownedSet)
    {
        if (RequireIds == null || RequireIds.Count == 0)
            return false;

        if (ownedSet == null)
            return false;

        int matchCount = 0;
        foreach (int requireId in RequireIds)
        {
            if (ownedSet.Contains(requireId))
            {
                matchCount++;
                if (matchCount >= RequireCount)
                    return true;
            }
        }

        return false;
    }
}
