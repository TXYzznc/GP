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
    public SynergyType Type; // 羁绊类型
    public string Description; // 羁绊描述
    public int RequireCount; // 激活所需数量
    public List<int> RequireIds; // 需要的物品/棋子ID列表
    public int EffectId; // 羁绊效果ID

    /// <summary>
    /// 检查是否满足激活条件
    /// </summary>
    public bool CheckActivation(List<int> ownedIds)
    {
        if (ownedIds == null || ownedIds.Count < RequireCount)
        {
            return false;
        }

        int matchCount = 0;
        foreach (int requireId in RequireIds)
        {
            if (ownedIds.Contains(requireId))
            {
                matchCount++;
                if (matchCount >= RequireCount)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
