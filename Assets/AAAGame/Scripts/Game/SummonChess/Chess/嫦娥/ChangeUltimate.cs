using UnityEngine;
using Cysharp.Threading.Tasks;
using GameExtension;

/// <summary>
/// 嫦娥大招：月华天倾 (ID=24)
/// 召唤月华之力，在目标位置生成法阵，法阵持续5秒释放子弹造成多段魔法伤害
/// 所有伤害逻辑由 ChangeMagicCircle 组件处理
/// </summary>
public class ChangeUltimate : ChessSkillBase
{
    #region 接口实现

    public override int SkillType => 4; // 大招
    #endregion

    #region 公共方法

    public override void Init(ChessContext ctx, SummonChessSkillTable config)
    {
        base.Init(ctx, config);
        DebugEx.LogModule("ChangeUltimate", "月华天倾初始化完成");
    }

    public override bool TryCast()
    {
        DebugEx.LogModule("ChangeUltimate", "→ 尝试释放大招「月华天倾」");

        if (!base.TryCast())
        {
            DebugEx.WarningModule("ChangeUltimate", "  ✗ 大招释放条件不满足（MP不足或冷却中）");
            return false;
        }

        DebugEx.LogModule(
            "ChangeUltimate",
            $"✓ 大招释放成功: 消耗MP={m_Config.MpCost}, 冷却时间={m_Config.Cooldown}秒"
        );

        return true;
    }

    /// <summary>
    /// 执行技能完整流程：异步加载法阵预制体并初始化
    /// </summary>
    public override void ExecuteSkill(ChessEntity caster)
    {
        DebugEx.LogModule("ChangeUltimate", "→ ExecuteSkill: 开始执行大招");

        if (caster == null)
        {
            DebugEx.ErrorModule("ChangeUltimate", "  ✗ 施法者为 null，无法执行技能");
            return;
        }

        DebugEx.LogModule("ChangeUltimate", $"  ├─ 施法者: {caster.Config?.Name}");

        // 播放技能释放特效（施法者位置）
        DebugEx.LogModule("ChangeUltimate", "  ├─ 播放技能释放特效...");
        PlaySkillEffect(caster);
        DebugEx.LogModule("ChangeUltimate", "  ├─ ✓ 特效播放完成");

        // 异步创建法阵
        DebugEx.LogModule("ChangeUltimate", "  └─ 启动异步创建法阵流程...");
        CreateMagicCircleAsync(caster).Forget();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 异步创建法阵
    /// </summary>
    private async UniTaskVoid CreateMagicCircleAsync(ChessEntity caster)
    {
        DebugEx.LogModule("ChangeUltimate", "→ 开始异步创建法阵...");

        // 获取目标敌人
        ChessEntity targetEnemy = FindNearestEnemy(caster);
        if (targetEnemy == null)
        {
            DebugEx.WarningModule("ChangeUltimate", "  ├─ ✗ 未找到目标敌人");
            return;
        }

        Vector3 targetPosition = targetEnemy.transform.position;
        DebugEx.LogModule("ChangeUltimate", $"  ├─ 目标敌人: {targetEnemy.Config?.Name}, 位置: {targetPosition}");

        // 从 CustomData 读取法阵预制体配置
        var customData = ParseCustomData(m_Config.CustomData);
        if (customData == null || customData.MagicCircleId <= 0)
        {
            DebugEx.ErrorModule("ChangeUltimate", "  ├─ ✗ CustomData 中未找到有效的 MagicCircleId");
            return;
        }

        int magicCirclePrefabId = customData.MagicCircleId;
        float projectileSpawnHeight = customData.SpawnHeight;

        DebugEx.LogModule("ChangeUltimate", $"  ├─ CustomData 解析成功: PrefabId={magicCirclePrefabId}, ProjectileSpawnHeight={projectileSpawnHeight}");

        // 异步加载法阵预制体
        DebugEx.LogModule("ChangeUltimate", $"  ├─ 正在加载法阵预制体 (ID={magicCirclePrefabId})...");
        GameObject prefab = await ResourceExtension.LoadPrefabAsync(magicCirclePrefabId);
        if (prefab == null)
        {
            DebugEx.ErrorModule("ChangeUltimate", $"  ├─ ✗ 法阵预制体加载失败 (ID={magicCirclePrefabId})");
            return;
        }
        DebugEx.LogModule("ChangeUltimate", $"  ├─ ✓ 法阵预制体加载成功");

        // 根据 SpawnHeight 计算法阵生成位置
        // SpawnHeight=0: 法阵底部对齐目标底部
        // SpawnHeight=1: 法阵底部对齐目标顶部
        Vector3 targetBottom = EntityPositionHelper.GetBottomPosition(targetEnemy);
        Vector3 targetTop = EntityPositionHelper.GetTopPosition(targetEnemy);
        Vector3 spawnPosition = Vector3.Lerp(targetBottom, targetTop, projectileSpawnHeight);

        DebugEx.LogModule(
            "ChangeUltimate",
            $"  ├─ 法阵生成位置计算:\n" +
            $"     ├─ 目标底部: {targetBottom}\n" +
            $"     ├─ 目标顶部: {targetTop}\n" +
            $"     ├─ SpawnHeight: {projectileSpawnHeight}\n" +
            $"     └─ 最终位置: {spawnPosition}"
        );

        // 实例化法阵
        DebugEx.LogModule("ChangeUltimate", $"  ├─ 实例化法阵预制体...");
        var circleInstance = Object.Instantiate(prefab, spawnPosition, Quaternion.identity);
        circleInstance.name = $"MagicCircle_{m_Config.Id}";
        DebugEx.LogModule("ChangeUltimate", $"  ├─ ✓ 法阵实例化成功，位置={spawnPosition}");

        // 获取并初始化 ChangeMagicCircle 组件
        var magicCircle = circleInstance.GetComponent<ChangeMagicCircle>();
        if (magicCircle == null)
        {
            DebugEx.ErrorModule("ChangeUltimate", "  ├─ ✗ 法阵预制体上未找到 ChangeMagicCircle 组件");
            Object.Destroy(circleInstance);
            return;
        }

        DebugEx.LogModule("ChangeUltimate", "  ├─ 初始化 ChangeMagicCircle 组件...");
        magicCircle.Initialize(m_Config, caster, spawnPosition);
        DebugEx.LogModule("ChangeUltimate", "  └─ ✓ 组件初始化成功");

        DebugEx.LogModule(
            "ChangeUltimate",
            $"✓ 法阵创建完成: " +
            $"技能={m_Config.Name}(ID={m_Config.Id}), " +
            $"预制体ID={magicCirclePrefabId}, " +
            $"生成位置={spawnPosition}, " +
            $"持续时间={m_Config.Duration}s, " +
            $"子弹数={m_Config.HitCount}发, " +
            $"AOE半径={m_Config.AreaRadius}米"
        );
    }

    /// <summary>
    /// 从 CustomData JSON 解析配置
    /// </summary>
    private CustomDataWrapper ParseCustomData(string customData)
    {
        if (string.IsNullOrEmpty(customData))
        {
            DebugEx.WarningModule("ChangeUltimate", "CustomData 为空");
            return null;
        }

        try
        {
            var data = JsonUtility.FromJson<CustomDataWrapper>(customData);
            return data;
        }
        catch (System.Exception ex)
        {
            DebugEx.ErrorModule("ChangeUltimate", $"✗ 解析 CustomData 失败: {customData}\n原因: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region 辅助类

    /// <summary>
    /// CustomData JSON 包装类
    /// 格式: {"MagicCircleId":3006,"ProjectilePrefabId":3007,"SpawnHeight":5}
    /// </summary>
    [System.Serializable]
    private class CustomDataWrapper
    {
        /// <summary>法阵预制体资源ID</summary>
        public int MagicCircleId;

        /// <summary>子弹预制体资源ID（法阵发射的投射物）</summary>
        public int ProjectilePrefabId;

        /// <summary>法阵生成相对位置（0=底部对齐，1=顶部对齐）</summary>
        public float SpawnHeight = 0f;
    }

    #endregion
}
