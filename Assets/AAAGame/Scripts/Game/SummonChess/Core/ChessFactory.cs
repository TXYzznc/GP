using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 棋子工厂类
/// 负责创建AI和技能实例
/// 参考 SkillFactory 的设计
/// </summary>
public static class ChessFactory
{
    #region AI创建

    /// <summary>AI创建器字典（AIType -> Creator）</summary>
    private static readonly Dictionary<int, Func<IChessAI>> s_AICreators = new Dictionary<int, Func<IChessAI>>();

    /// <summary>
    /// 注册所有AI类型
    /// 在游戏启动时调用一次
    /// </summary>
    public static void RegisterAllAI()
    {
        s_AICreators.Clear();

        // 旧版反应式AI（保留用于对比测试）
        RegisterAI(3, () => new DummyAI());      // 假人测试AI

        // ✅ 新版状态机AI
        RegisterAI(1, () => new FSMMeleeAI());  // 近战AI（状态机版）
        RegisterAI(2, () => new FSMRangedAI()); // 远程AI（状态机版）

        // 后续可扩展更多AI类型
        // RegisterAI(13, () => new FSMTankAI());
        // RegisterAI(14, () => new FSMSupportAI());

        DebugEx.LogModule("ChessFactory", $"已注册 {s_AICreators.Count} 个AI类型");
    }

    /// <summary>
    /// 注册AI类型
    /// </summary>
    /// <param name="aiType">AI类型ID</param>
    /// <param name="creator">创建器函数</param>
    public static void RegisterAI(int aiType, Func<IChessAI> creator)
    {
        if (creator == null)
        {
            DebugEx.Error($"ChessFactory.RegisterAI: creator is null, aiType={aiType}");
            return;
        }

        if (s_AICreators.ContainsKey(aiType))
        {
            DebugEx.Warning($"ChessFactory.RegisterAI: AI类型 {aiType} 已注册，将被覆盖");
        }

        s_AICreators[aiType] = creator;
    }

    /// <summary>
    /// 创建AI实例
    /// </summary>
    /// <param name="aiType">AI类型ID</param>
    /// <returns>AI实例，如果类型未注册则返回null</returns>
    public static IChessAI CreateAI(int aiType)
    {
        if (s_AICreators.TryGetValue(aiType, out var creator))
        {
            try
            {
                var ai = creator();
                if (ai != null)
                {
                    DebugEx.Log($"ChessFactory: 创建AI成功 (Type={aiType})");
                    return ai;
                }
                else
                {
                    DebugEx.Error($"ChessFactory: AI创建器返回null (Type={aiType})");
                    return null;
                }
            }
            catch (Exception e)
            {
                DebugEx.Error($"ChessFactory: 创建AI失败 (Type={aiType}): {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        DebugEx.Warning($"ChessFactory: AI类型 {aiType} 未注册");
        return null;
    }

    /// <summary>
    /// 检查AI类型是否已注册
    /// </summary>
    /// <param name="aiType">AI类型ID</param>
    /// <returns>是否已注册</returns>
    public static bool HasAI(int aiType)
    {
        return s_AICreators.ContainsKey(aiType);
    }

    /// <summary>
    /// 获取已注册AI类型数量
    /// </summary>
    /// <returns>AI类型数量</returns>
    public static int GetAICount()
    {
        return s_AICreators.Count;
    }

    #endregion

    #region 技能创建

    /// <summary>技能创建器字典（SkillId -> Creator）</summary>
    private static readonly Dictionary<int, Func<IChessSkill>> s_SkillCreators = new Dictionary<int, Func<IChessSkill>>();

    /// <summary>
    /// 注册所有技能
    /// 在游戏启动时调用一次
    /// </summary>
    public static void RegisterAllSkills()
    {
        s_SkillCreators.Clear();

        // 后羿技能
        RegisterSkill(13, () => new HouyiSkill1());
        RegisterSkill(14, () => new HouyiUltimate());

        // 嫦娥技能
        RegisterSkill(23, () => new ChangeSkill1());
        RegisterSkill(24, () => new ChangeUltimate());

        DebugEx.Log($"ChessFactory: 已注册 {s_SkillCreators.Count} 个棋子技能");
    }

    /// <summary>
    /// 注册技能
    /// </summary>
    /// <param name="skillId">技能ID</param>
    /// <param name="creator">创建器函数</param>
    public static void RegisterSkill(int skillId, Func<IChessSkill> creator)
    {
        if (creator == null)
        {
            DebugEx.Error($"ChessFactory.RegisterSkill: creator is null, skillId={skillId}");
            return;
        }

        if (s_SkillCreators.ContainsKey(skillId))
        {
            DebugEx.Warning($"ChessFactory.RegisterSkill: 技能 {skillId} 已注册，将被覆盖");
        }

        s_SkillCreators[skillId] = creator;
    }

    /// <summary>
    /// 创建技能实例
    /// </summary>
    /// <param name="skillId">技能ID</param>
    /// <returns>技能实例，如果ID为0或未注册则返回null</returns>
    public static IChessSkill CreateSkill(int skillId)
    {
        // 技能ID为0表示无技能
        if (skillId == 0)
        {
            return null;
        }

        if (s_SkillCreators.TryGetValue(skillId, out var creator))
        {
            try
            {
                var skill = creator();
                if (skill != null)
                {
                    DebugEx.Log($"ChessFactory: 创建技能成功 (Id={skillId})");
                    return skill;
                }
                else
                {
                    DebugEx.Error($"ChessFactory: 技能创建器返回null (Id={skillId})");
                    return null;
                }
            }
            catch (Exception e)
            {
                DebugEx.Error($"ChessFactory: 创建技能失败 (Id={skillId}): {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        DebugEx.Warning($"ChessFactory: 技能 {skillId} 未注册");
        return null;
    }

    /// <summary>
    /// 检查技能是否已注册
    /// </summary>
    /// <param name="skillId">技能ID</param>
    /// <returns>是否已注册</returns>
    public static bool HasSkill(int skillId)
    {
        return s_SkillCreators.ContainsKey(skillId);
    }

    /// <summary>
    /// 获取已注册的技能数量
    /// </summary>
    /// <returns>技能数量</returns>
    public static int GetSkillCount()
    {
        return s_SkillCreators.Count;
    }

    #endregion

    #region 技能策略创建

    // 技能策略注册表
    private static Dictionary<int, System.Type> s_SkillStrategyRegistry = new Dictionary<int, System.Type>();

    /// <summary>
    /// 注册所有技能释放策略
    /// 在游戏启动时调用一次
    /// </summary>
    public static void RegisterAllSkillStrategies()
    {
        s_SkillStrategyRegistry.Clear();

        // 注册后羿的技能策略
        //RegisterSkillStrategy(1, typeof(HouyiSkillReleaseStrategy));

        // 注册嫦娥的技能策略
        //RegisterSkillStrategy(4, typeof(ChangESkillReleaseStrategy));

        // 其他棋子使用默认策略（不需要注册）

        DebugEx.LogModule("ChessFactory",
            $"已注册 {s_SkillStrategyRegistry.Count} 个技能策略");
    }

    /// <summary>
    /// 注册技能释放策略
    /// </summary>
    public static void RegisterSkillStrategy(int chessId, System.Type strategyType)
    {
        if (!typeof(ISkillReleaseStrategy).IsAssignableFrom(strategyType))
        {
            DebugEx.ErrorModule("ChessFactory",
                $"策略类型 {strategyType.Name} 必须实现 ISkillReleaseStrategy 接口");
            return;
        }

        s_SkillStrategyRegistry[chessId] = strategyType;
        DebugEx.LogModule("ChessFactory",
            $"注册技能策略: ChessId={chessId}, Strategy={strategyType.Name}");
    }

    /// <summary>
    /// 创建技能释放策略
    /// </summary>
    public static ISkillReleaseStrategy CreateSkillStrategy(int chessId, ChessContext context)
    {
        ISkillReleaseStrategy strategy;

        if (s_SkillStrategyRegistry.TryGetValue(chessId, out System.Type strategyType))
        {
            strategy = System.Activator.CreateInstance(strategyType) as ISkillReleaseStrategy;
        }
        else
        {
            // 使用默认策略
            strategy = new DefaultSkillReleaseStrategy();
        }

        strategy.Init(context);
        return strategy;
    }

    #endregion

    #region 被动创建

    /// <summary>被动创建器字典（PassiveId -> Creator）</summary>
    private static readonly Dictionary<int, Func<IChessPassive>> s_PassiveCreators = new Dictionary<int, Func<IChessPassive>>();

    /// <summary>
    /// 注册所有被动技能
    /// </summary>
    public static void RegisterAllPassives()
    {
        s_PassiveCreators.Clear();

        // 后羿被动
        RegisterPassive(11, () => new HouyiPassive());

        // 嫦娥被动
        RegisterPassive(21, () => new ChangePassive());

        DebugEx.Log($"ChessFactory: 已注册 {s_PassiveCreators.Count} 个被动技能");
    }

    /// <summary>
    /// 注册被动技能
    /// </summary>
    public static void RegisterPassive(int passiveId, Func<IChessPassive> creator)
    {
        if (creator == null)
        {
            DebugEx.Error($"ChessFactory.RegisterPassive: creator is null, passiveId={passiveId}");
            return;
        }

        if (s_PassiveCreators.ContainsKey(passiveId))
        {
            DebugEx.Warning($"ChessFactory.RegisterPassive: 被动 {passiveId} 已注册，将被覆盖");
        }

        s_PassiveCreators[passiveId] = creator;
    }

    /// <summary>
    /// 创建被动技能实例
    /// </summary>
    public static IChessPassive CreatePassive(int passiveId)
    {
        if (passiveId == 0) return null;

        if (s_PassiveCreators.TryGetValue(passiveId, out var creator))
        {
            try
            {
                var passive = creator();
                if (passive != null)
                {
                    DebugEx.Log($"ChessFactory: 创建被动成功 (Id={passiveId})");
                    return passive;
                }
            }
            catch (Exception e)
            {
                DebugEx.Error($"ChessFactory: 创建被动失败 (Id={passiveId}): {e.Message}");
            }
        }

        DebugEx.Warning($"ChessFactory: 被动 {passiveId} 未注册");
        return null;
    }

    #endregion

    #region 普攻创建

    /// <summary>普攻创建器字典（AttackId -> Creator）</summary>
    private static readonly Dictionary<int, Func<IChessNormalAttack>> s_NormalAtkCreators = new Dictionary<int, Func<IChessNormalAttack>>();

    /// <summary>
    /// 注册所有普攻效果
    /// </summary>
    public static void RegisterAllNormalAttacks()
    {
        s_NormalAtkCreators.Clear();

        // 后羿普攻
        RegisterNormalAttack(12, () => new HouyiNormalAttack());

        // 嫦娥普攻
        RegisterNormalAttack(22, () => new ChangeNormalAttack());

        DebugEx.Log($"ChessFactory: 已注册 {s_NormalAtkCreators.Count} 个普攻效果");
    }

    /// <summary>
    /// 注册普攻效果
    /// </summary>
    public static void RegisterNormalAttack(int attackId, Func<IChessNormalAttack> creator)
    {
        if (creator == null)
        {
            DebugEx.Error($"ChessFactory.RegisterNormalAttack: creator is null, attackId={attackId}");
            return;
        }

        if (s_NormalAtkCreators.ContainsKey(attackId))
        {
            DebugEx.Warning($"ChessFactory.RegisterNormalAttack: 普攻 {attackId} 已注册，将被覆盖");
        }

        s_NormalAtkCreators[attackId] = creator;
    }

    /// <summary>
    /// 创建普攻效果实例
    /// </summary>
    public static IChessNormalAttack CreateNormalAttack(int attackId)
    {
        if (attackId == 0) return null;

        if (s_NormalAtkCreators.TryGetValue(attackId, out var creator))
        {
            try
            {
                var attack = creator();
                if (attack != null)
                {
                    DebugEx.Log($"ChessFactory: 创建普攻效果成功 (Id={attackId})");
                    return attack;
                }
            }
            catch (Exception e)
            {
                DebugEx.Error($"ChessFactory: 创建普攻效果失败 (Id={attackId}): {e.Message}");
            }
        }

        DebugEx.Warning($"ChessFactory: 普攻效果 {attackId} 未注册");
        return null;
    }

    #endregion

    #region 调试方法

    /// <summary>
    /// 打印所有已注册的AI类型（调试用）
    /// </summary>
    public static void DebugPrintAllAI()
    {
        DebugEx.Log($"=== ChessFactory 已注册AI类型 (共{s_AICreators.Count}个) ===");
        foreach (var kvp in s_AICreators)
        {
            DebugEx.Log($"AI Type: {kvp.Key}");
        }
        DebugEx.Log("==========================================");
    }

    /// <summary>
    /// 打印所有已注册的技能（调试用）
    /// </summary>
    public static void DebugPrintAllSkills()
    {
        DebugEx.Log($"=== ChessFactory 已注册技能 (共{s_SkillCreators.Count}个) ===");
        foreach (var kvp in s_SkillCreators)
        {
            DebugEx.Log($"Skill Id: {kvp.Key}");
        }
        DebugEx.Log("==========================================");
    }

    /// <summary>
    /// 获取调试信息
    /// </summary>
    public static string GetDebugInfo()
    {
        return $"[ChessFactory] AI={s_AICreators.Count}, 技能={s_SkillCreators.Count}, 被动={s_PassiveCreators.Count}, 普攻={s_NormalAtkCreators.Count}";
    }

    #endregion
}
