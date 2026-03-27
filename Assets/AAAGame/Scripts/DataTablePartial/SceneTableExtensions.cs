using System.Collections.Generic;
using UnityGameFramework.Runtime;

/// <summary>
/// SceneTable 扩展方法（静态扩展）
/// </summary>
public static class SceneTableExtensions
{
    #region 辅助方法
    
    /// <summary>
    /// 获取场景类型枚举
    /// </summary>
    public static SceneType GetSceneTypeEnum(this SceneTable table)
    {
        if (table == null)
            return SceneType.Unknown;
            
        return (SceneType)table.SceneType;
    }
    
    /// <summary>
    /// 获取条件类型枚举
    /// </summary>
    public static SceneConditionType GetConditionTypeEnum(this SceneTable table)
    {
        if (table == null)
            return SceneConditionType.None;
            
        return (SceneConditionType)table.ConditionType;
    }
    
    /// <summary>
    /// 解析条件参数为整数（用于任务ID、等级、物品ID等）
    /// </summary>
    public static int GetConditionParamAsInt(this SceneTable table)
    {
        if (table == null || string.IsNullOrEmpty(table.ConditionParam))
            return 0;
            
        if (int.TryParse(table.ConditionParam, out int result))
            return result;
            
        return 0;
    }
    
    /// <summary>
    /// 解析条件参数为整数列表（用于多个任务ID、多个物品ID等）
    /// 格式：1,2,3,4
    /// </summary>
    public static List<int> GetConditionParamAsIntList(this SceneTable table)
    {
        var result = new List<int>();
        
        if (table == null || string.IsNullOrEmpty(table.ConditionParam))
            return result;
            
        string[] parts = table.ConditionParam.Split(',');
        foreach (var part in parts)
        {
            if (int.TryParse(part.Trim(), out int value))
            {
                result.Add(value);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// 检查是否满足进入条件
    /// </summary>
    public static bool CheckCondition(this SceneTable table, PlayerSaveData saveData)
    {
        if (table == null || saveData == null)
            return false;
            
        var conditionType = table.GetConditionTypeEnum();
        
        switch (conditionType)
        {
            case SceneConditionType.None:
                return true;
                
            case SceneConditionType.CompleteTutorial:
                return saveData.HasCompletedTutorial;
                
            case SceneConditionType.CompleteQuest:
                var requiredQuestIds = table.GetConditionParamAsIntList();
                foreach (var questId in requiredQuestIds)
                {
                    if (!saveData.CompletedQuestIds.Contains(questId))
                    {
                        Log.Info($"SceneTable: 缺少任务 {questId}");
                        return false;
                    }
                }
                return true;
                
            case SceneConditionType.ReachLevel:
                int requiredLevel = table.GetConditionParamAsInt();
                return saveData.GlobalLevel >= requiredLevel;
                
            case SceneConditionType.HasItem:
                var requiredItemIds = table.GetConditionParamAsIntList();
                var inventoryItems = saveData.GetInventoryItems();
                foreach (var itemId in requiredItemIds)
                {
                    bool hasItem = inventoryItems.Exists(item => item.ItemId == itemId);
                    if (!hasItem)
                    {
                        Log.Info($"SceneTable: 缺少物品 {itemId}");
                        return false;
                    }
                }
                return true;
                
            case SceneConditionType.UnlockTech:
                var requiredTechIds = table.GetConditionParamAsIntList();
                foreach (var techId in requiredTechIds)
                {
                    if (!saveData.UnlockedTechIds.Contains(techId))
                    {
                        Log.Info($"SceneTable: 缺少科技 {techId}");
                        return false;
                    }
                }
                return true;
                
            case SceneConditionType.Custom:
                // 自定义条件需要在 SceneStateManager 中实现
                Log.Warning($"SceneTable: 自定义条件 {table.ConditionParam} 需要在代码中实现");
                return false;
                
            default:
                Log.Warning($"SceneTable: 未知条件类型 {conditionType}");
                return false;
        }
    }
    
    /// <summary>
    /// 获取条件不满足的提示文本
    /// </summary>
    public static string GetConditionNotMetMessage(this SceneTable table)
    {
        if (table == null)
            return "场景信息无效";
            
        var conditionType = table.GetConditionTypeEnum();
        
        switch (conditionType)
        {
            case SceneConditionType.CompleteTutorial:
                return "需要完成新手引导才能进入此区域";
                
            case SceneConditionType.CompleteQuest:
                return "需要完成指定任务才能进入此区域";
                
            case SceneConditionType.ReachLevel:
                int requiredLevel = table.GetConditionParamAsInt();
                return $"需要达到 {requiredLevel} 级才能进入此区域";
                
            case SceneConditionType.HasItem:
                return "需要特定物品才能进入此区域";
                
            case SceneConditionType.UnlockTech:
                return "需要解锁特定科技才能进入此区域";
                
            case SceneConditionType.Custom:
                return "不满足特殊进入条件";
                
            default:
                return "不满足进入条件";
        }
    }
    
    #endregion
}
