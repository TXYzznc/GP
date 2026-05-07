using Cysharp.Threading.Tasks;
using GameExtension;
using UnityEngine;

/// <summary>
/// 邪灵技能二：腐蚀法阵 (ID=34)
/// 锁定目标位置生成法阵：
/// - 敌人首次进入法阵：立刻获得2层腐蚀
/// - 处于法阵内：持续受到魔法伤害；每停留5秒叠加1层腐蚀（离开重置计时）
/// - 法阵爆炸：对范围内敌人造成大量伤害，并再施加1层腐蚀（由特效动画帧事件触发）
/// </summary>
public class EvilSpiritUltimate : ChessSkillBase
{
    #region 接口实现

    public override int SkillType => 4; // 大招
    #endregion

    #region 公共方法

    public override void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        base.Init(ctx, config);
        DebugEx.Log("EvilSpiritUltimate", "腐蚀法阵技能初始化完成");
    }

    public override bool TryCast()
    {
        if (!base.TryCast())
            return false;

        DebugEx.Log("EvilSpiritUltimate", "腐蚀法阵技能释放");
        return true;
    }

    public override void ExecuteSkill(ChessEntity caster)
    {
        if (caster == null)
        {
            DebugEx.Error("EvilSpiritUltimate", "ExecuteSkill: caster 为 null");
            return;
        }

        CreateMagicCircleAsync(caster).Forget();
    }

    #endregion

    #region 私有方法

    private async UniTaskVoid CreateMagicCircleAsync(ChessEntity caster)
    {
        if (caster == null || caster.CurrentState == ChessState.Dead)
            return;

        // ⭐ 获取技能目标（由 AI 的技能释放策略决定）
        var aiBase = caster.AI as ChessAIBase;

        // 如果是通过位置目标（最密集位置），则使用 FindMostDensePosition
        // 否则使用技能目标棋子的位置
        Vector3 targetPosition;
        if (aiBase != null && aiBase.SkillTarget != null)
        {
            // 有具体的目标棋子（备用方案）
            targetPosition = aiBase.SkillTarget.transform.position;
        }
        else
        {
            // 没有目标，使用最密集位置策略
            targetPosition = ChessTargetFinder.FindMostDensePosition(
                caster,
                (float)m_Config.CastRange + 4f  // 施法范围 + 4
            );
        }

        var customData = ParseCustomData(m_Config.CustomData);
        if (customData == null || customData.MagicCircleId <= 0)
        {
            DebugEx.Error("EvilSpiritUltimate", "腐蚀法阵：CustomData 中未找到有效的 MagicCircleId");
            return;
        }

        // ⭐ 使用 targetPosition 作为生成位置
        // 如果有具体目标，targetPosition 是目标的位置；如果只有位置策略，则直接使用最密集位置
        Vector3 spawnPosition = new Vector3(targetPosition.x, caster.transform.position.y, targetPosition.z);

        GameObject prefab = await ResourceExtension.LoadPrefabAsync(customData.MagicCircleId);
        if (prefab == null)
        {
            DebugEx.Error("EvilSpiritUltimate", $"腐蚀法阵：加载法阵预制体失败 MagicCircleId={customData.MagicCircleId}");
            return;
        }

        GameObject circleInstance = Object.Instantiate(prefab, spawnPosition, Quaternion.identity);
        Animator animator = circleInstance.GetComponentInChildren<Animator>(true);
        Animation legacyAnimation = circleInstance.GetComponentInChildren<Animation>(true);

        var magicCircle = circleInstance.GetComponent<EvilSpiritMagicCircle>();
        if (magicCircle == null)
            magicCircle = circleInstance.AddComponent<EvilSpiritMagicCircle>();
        magicCircle.Initialize(m_Config, caster, spawnPosition);

        GameObject eventHost = null;
        if (animator != null)
            eventHost = animator.gameObject;
        else if (legacyAnimation != null)
            eventHost = legacyAnimation.gameObject;

        if (eventHost == null)
        {
            DebugEx.Error("EvilSpiritUltimate", "腐蚀法阵：法阵预制体缺少 Animator/Animation，无法通过动画帧事件驱动");
            Object.Destroy(circleInstance);
            return;
        }

        var bridge = eventHost.GetComponent<EvilSpiritMagicCircleAnimBridge>();
        if (bridge == null)
            bridge = eventHost.AddComponent<EvilSpiritMagicCircleAnimBridge>();
        bridge.Bind(magicCircle);

        if (animator == null && legacyAnimation != null)
        {
            string clipName = legacyAnimation.clip != null ? legacyAnimation.clip.name : null;
            if (string.IsNullOrEmpty(clipName))
            {
                foreach (AnimationState state in legacyAnimation)
                {
                    clipName = state.name;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(clipName))
            {
                legacyAnimation.Play(clipName);
            }
            else
            {
                DebugEx.Error("EvilSpiritUltimate", "腐蚀法阵：Animation 组件未配置任何 Clip，无法播放也无法触发帧事件");
            }
        }

        DebugEx.Log("EvilSpiritUltimate", $"腐蚀法阵已生成: center={spawnPosition}, radius={m_Config.AreaRadius}, prefabId={customData.MagicCircleId}");
    }

    private CustomDataWrapper ParseCustomData(string customData)
    {
        if (string.IsNullOrEmpty(customData) || customData == "{}")
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<CustomDataWrapper>(customData);
        }
        catch (System.Exception)
        {
            return null;
        }
    }

    #endregion

    [System.Serializable]
    private class CustomDataWrapper
    {
        public int MagicCircleId;
        public float SpawnHeight = 0f;
    }
}
