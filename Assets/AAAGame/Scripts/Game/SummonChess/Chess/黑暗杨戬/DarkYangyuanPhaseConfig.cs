using System;
using System.Collections.Generic;
using UnityGameFramework.Runtime;
using Newtonsoft.Json;

/// <summary>
/// 黑暗杨戬多阶段配置解析类
/// 从 CustomData JSON 中解析阶段信息
/// </summary>
public class DarkYangyuanPhaseConfig
{
    public List<PhaseData> Phases { get; set; } = new List<PhaseData>();

    public PhaseData GetPhase(int phaseNum)
    {
        return Phases.Find(p => p.PhaseNum == phaseNum);
    }

    public static DarkYangyuanPhaseConfig ParseFromCustomData(string customData)
    {
        if (string.IsNullOrEmpty(customData))
        {
            DebugEx.Error("DarkYangyuanPhaseConfig", "CustomData 为空");
            return null;
        }

        try
        {
            var config = JsonConvert.DeserializeObject<DarkYangyuanPhaseConfig>(customData);
            return config;
        }
        catch (Exception ex)
        {
            DebugEx.Error("DarkYangyuanPhaseConfig", $"CustomData 解析失败: {ex.Message}");
            return null;
        }
    }

    [Serializable]
    public class PhaseData
    {
        public int PhaseNum { get; set; }
        public double HealthPercentThreshold { get; set; }
        public List<int> BuffIds { get; set; } = new List<int>();
        public Dictionary<string, int> SkillOverride { get; set; }
        public Dictionary<string, float> UltimateModifier { get; set; }
        public SubordinateSpawnData SubordinateSpawn { get; set; }
    }

    [Serializable]
    public class SubordinateSpawnData
    {
        public int ChessId { get; set; }
        public int Quantity { get; set; }
        public double InheritRatio { get; set; }
    }
}
