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

        ChessEntity targetEnemy = FindNearestEnemy(caster);
        if (targetEnemy == null)
        {
            DebugEx.Warning("EvilSpiritUltimate", "腐蚀法阵：未找到可锁定目标");
            return;
        }

        Vector3 targetPosition = targetEnemy.transform.position;

        var customData = ParseCustomData(m_Config.CustomData);
        if (customData == null || customData.MagicCircleId <= 0)
        {
            DebugEx.Error("EvilSpiritUltimate", "腐蚀法阵：CustomData 中未找到有效的 MagicCircleId");
            return;
        }

        Vector3 targetBottom = EntityPositionHelper.GetBottomPosition(targetEnemy);
        Vector3 targetTop = EntityPositionHelper.GetTopPosition(targetEnemy);
        Vector3 spawnPosition = Vector3.Lerp(targetBottom, targetTop, customData.SpawnHeight);

        GameObject prefab = await ResourceExtension.LoadPrefabAsync(customData.MagicCircleId);

        GameObject circleInstance = null;
        Animator animator = null;
        if (prefab != null)
        {
            circleInstance = Object.Instantiate(prefab, spawnPosition, Quaternion.identity);
            animator = circleInstance.GetComponentInChildren<Animator>(true);
        }

        if (circleInstance == null)
        {
            circleInstance = new GameObject("EvilSpiritMagicCircle_Runtime");
            circleInstance.transform.position = spawnPosition;
        }

        var magicCircle = circleInstance.GetComponent<EvilSpiritMagicCircle>();
        if (magicCircle == null)
            magicCircle = circleInstance.AddComponent<EvilSpiritMagicCircle>();
        magicCircle.Initialize(m_Config, caster, spawnPosition);

        if (animator == null)
        {
            magicCircle.BeginAutoLifecycleFromConfig();
        }
        else
        {
            var bridge = animator.gameObject.GetComponent<EvilSpiritMagicCircleAnimBridge>();
            if (bridge == null)
                bridge = animator.gameObject.AddComponent<EvilSpiritMagicCircleAnimBridge>();
            bridge.Bind(magicCircle);
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
