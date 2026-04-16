using UnityEngine;
using Cysharp.Threading.Tasks;
using GameExtension;

/// <summary>
/// 嫦娥法阵效果组件
/// 自管理生命周期和子弹发射
/// </summary>
public class ChangeMagicCircle : MonoBehaviour
{
    #region 私有字段

    /// <summary>技能配置</summary>
    private SummonChessSkillTable m_Config;

    /// <summary>施法者</summary>
    private ChessEntity m_Caster;

    /// <summary>法阵剩余时间</summary>
    private float m_RemainingTime;

    /// <summary>子弹发射计时器</summary>
    private float m_ProjectileTimer;

    /// <summary>子弹发射间隔</summary>
    private float m_ProjectileInterval;

    /// <summary>已发射子弹数量</summary>
    private int m_ProjectilesFired;

    /// <summary>单发伤害</summary>
    private double m_ProjectileDamage;

    /// <summary>子弹生成平面（在预制体中设置）</summary>
    [SerializeField]private Collider m_SpawnPlane;

    /// <summary>自定义数据（包含投射物预制体 ID 等）</summary>
    private CustomDataWrapper m_CustomData;

    /// <summary>是否已初始化</summary>
    private bool m_IsInitialized;

    #endregion

    #region 公共方法

    /// <summary>
    /// 初始化法阵
    /// </summary>
    /// <param name="config">技能配置</param>
    /// <param name="caster">施法者</param>
    /// <param name="targetPosition">目标位置</param>
    public void Initialize(SummonChessSkillTable config, ChessEntity caster, Vector3 targetPosition)
    {
        DebugEx.LogModule("ChangeMagicCircle", "→ 开始初始化法阵组件...");

        m_Config = config;
        m_Caster = caster;
        m_RemainingTime = (float)config.Duration;

        // 计算子弹发射间隔
        m_ProjectileInterval = config.HitCount > 0 ? (float)config.Duration / config.HitCount : 1f;

        // ⭐ 设置初始延迟，避免第一帧就发射子弹
        m_ProjectileTimer = -0.1f; // 延迟 0.1 秒后开始发射第一枚子弹

        // 计算单发伤害
        var selfAttr = caster.Attribute;
        double scalingStat = selfAttr.SpellPower > 0 ? selfAttr.SpellPower : selfAttr.AtkDamage;
        m_ProjectileDamage = scalingStat * config.DamageCoeff + config.BaseDamage;

        DebugEx.LogModule(
            "ChangeMagicCircle",
            $"  ├─ 伤害计算: {scalingStat:F1}×{config.DamageCoeff}+{config.BaseDamage}={m_ProjectileDamage:F1}"
        );

        // 从 CustomData 读取配置（子弹预制体 ID、法阵生成高度等）
        m_CustomData = ParseCustomData(config.CustomData);
        if (m_CustomData != null && m_CustomData.ProjectilePrefabId > 0)
        {
            DebugEx.LogModule("ChangeMagicCircle", $"  ├─ ✓ 从 CustomData 读取配置: ProjectilePrefabId={m_CustomData.ProjectilePrefabId}, SpawnHeight={m_CustomData.SpawnHeight:F2}");
        }
        else
        {
            DebugEx.LogModule("ChangeMagicCircle", $"  ├─ ⚠ CustomData 为空或无效，无法加载子弹预制体");
        }

        // 设置法阵位置（根据 SpawnHeight 调整相对于目标的垂直位置）
        float spawnHeightOffset = m_CustomData != null ? m_CustomData.SpawnHeight : 0f;
        Vector3 magicCirclePos = targetPosition + Vector3.up * spawnHeightOffset;
        transform.position = magicCirclePos;
        DebugEx.LogModule("ChangeMagicCircle", $"  ├─ 法阵位置: {magicCirclePos} (目标位置相对高度: {spawnHeightOffset:F2})");

        // 获取子弹生成平面
        m_SpawnPlane = GetComponentInChildren<Collider>();
        if (m_SpawnPlane != null)
        {
            DebugEx.LogModule("ChangeMagicCircle", $"  ├─ ✓ 获取子弹生成平面: {m_SpawnPlane.gameObject.name}");
        }
        else
        {
            DebugEx.WarningModule("ChangeMagicCircle", $"  ├─ ⚠ 未找到子弹生成平面，请在预制体中添加平面 Collider");
        }

        // ⭐ 在法阵位置播放特效
        if (m_Config.EffectId > 0)
        {
            DebugEx.LogModule(
                "ChangeMagicCircle",
                $"  ├─ 播放法阵特效: ID={m_Config.EffectId}, 高度={m_Config.EffectSpawnHeight}"
            );
            CombatVFXManager.PlayEffect(m_Config.EffectId, targetPosition);
        }
        else
        {
            DebugEx.WarningModule("ChangeMagicCircle", "  ├─ ⚠ 未配置法阵特效 (EffectId=0)");
        }

        m_IsInitialized = true;

        DebugEx.LogModule(
            "ChangeMagicCircle",
            $"✓ 法阵初始化完成:\n" +
            $"  ├─ 持续时间: {config.Duration}s\n" +
            $"  ├─ 子弹总数: {config.HitCount}发\n" +
            $"  ├─ 发射间隔: {m_ProjectileInterval:F2}s\n" +
            $"  ├─ 单发伤害: {m_ProjectileDamage:F1}\n" +
            $"  ├─ 伤害类型: {(config.DamageType == 2 ? "魔法" : config.DamageType == 3 ? "真实" : "物理")}\n" +
            $"  └─ AOE半径: {config.AreaRadius}米"
        );

        DebugEx.LogModule(
            "ChangeMagicCircle",
            $"→ 法阵开始运作: 将在 {config.Duration}s 内发射 {config.HitCount} 枚子弹，首发延迟 0.1s"
        );
    }

    #endregion

    #region Unity 生命周期

    private void Update()
    {
        if (!m_IsInitialized)
            return;

        float deltaTime = Time.deltaTime;

        // 更新剩余时间
        m_RemainingTime -= deltaTime;

        // 更新子弹发射计时器
        m_ProjectileTimer += deltaTime;

        // ⭐ 优化子弹发射逻辑：确保所有子弹都能发射
        bool shouldFire = false;

        if (m_ProjectilesFired < m_Config.HitCount)
        {
            // 正常间隔发射
            if (m_ProjectileTimer >= m_ProjectileInterval)
            {
                shouldFire = true;
            }
            // ⭐ 法阵即将结束时，强制发射剩余子弹
            else if (m_RemainingTime <= 0.1f && m_ProjectileTimer > 0f)
            {
                shouldFire = true;
            }
        }

        if (shouldFire)
        {
            FireProjectile();
            m_ProjectileTimer = 0f; // ⭐ 重置为0，而不是减去间隔
            m_ProjectilesFired++;
        }

        // 检查是否结束（所有子弹发射完毕或时间耗尽）
        if (m_RemainingTime <= 0f || m_ProjectilesFired >= m_Config.HitCount)
        {
            OnComplete();
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 发射子弹
    /// </summary>
    private void FireProjectile()
    {
        // ⭐ 检查施法者是否仍然有效
        if (m_Caster == null || m_Caster.CurrentState == ChessState.Dead)
        {
            DebugEx.WarningModule("ChangeMagicCircle", "施法者已死亡或无效，停止发射子弹");
            OnComplete();
            return;
        }

        // 子弹起始位置：在平面内随机生成
        Vector3 startPosition;
        Vector3 targetPosition;

        if (m_SpawnPlane != null)
        {
            // 获取平面的边界
            Bounds bounds = m_SpawnPlane.bounds;
            float spawnHeight = bounds.center.y;

            // 在平面范围内随机生成X、Z坐标
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);
            startPosition = new Vector3(randomX, spawnHeight, randomZ);

            // 目标位置：竖直向下（设置一个很低的高度）
            targetPosition = startPosition + Vector3.down * 100f;
        }
        else
        {
            // 降级处理：如果未配置平面，子弹无法生成
            DebugEx.WarningModule("ChangeMagicCircle", "  └─ ✗ 子弹生成平面未设置，无法发射子弹");
            return;
        }

        // 计算本次伤害（可能暴击）
        double damage = m_ProjectileDamage;
        bool isCritical = Random.value < m_Caster.Attribute.CritRate;
        if (isCritical)
        {
            damage *= m_Caster.Attribute.CritDamage;
        }

        // ⭐ 特殊类型技能：直接加载并发射子弹预制体
        LoadAndFireProjectileAsync(startPosition, targetPosition, damage, isCritical).Forget();

        string directionDesc = m_SpawnPlane != null ? "竖直向下" : "指向法阵中心";
        DebugEx.LogModule(
            "ChangeMagicCircle",
            $"→ 子弹 {m_ProjectilesFired + 1}/{m_Config.HitCount} 发射:\n" +
            $"  ├─ 发射位置: {startPosition}\n" +
            $"  ├─ 移动方向: {directionDesc}\n" +
            $"  ├─ 伤害: {damage:F1}{(isCritical ? " (暴击!)" : "")}\n" +
            $"  ├─ 剩余时间: {m_RemainingTime:F2}s\n" +
            $"  └─ 特殊类型（异步加载投射物）"
        );
    }

    /// <summary>
    /// 异步加载并发射子弹
    /// </summary>
    private async UniTaskVoid LoadAndFireProjectileAsync(Vector3 startPos, Vector3 targetPos, double damage, bool isCritical)
    {
        // 从 CustomData 读取子弹预制体 ID
        if (m_CustomData == null || m_CustomData.ProjectilePrefabId <= 0)
        {
            DebugEx.WarningModule("ChangeMagicCircle", "  └─ ⚠ 未配置投射物预制体 (ProjectilePrefabId)");
            return;
        }

        // 异步加载子弹预制体
        GameObject bulletPrefab = await ResourceExtension.LoadPrefabAsync(m_CustomData.ProjectilePrefabId);
        if (bulletPrefab == null)
        {
            DebugEx.WarningModule("ChangeMagicCircle", $"  └─ ✗ 子弹预制体加载失败 (ID={m_CustomData.ProjectilePrefabId})");
            return;
        }

        // ⭐ 调试：打印预制体的详细信息
        Component[] allComponents = bulletPrefab.GetComponents<Component>();
        string componentList = string.Join(", ", System.Array.ConvertAll(allComponents, c => c.GetType().Name));
        DebugEx.LogModule("ChangeMagicCircle", $"  ├─ 加载的预制体信息: ID={m_CustomData.ProjectilePrefabId}, Name={bulletPrefab.name}, Components=[{componentList}]");

        // 实例化子弹
        GameObject bulletObj = Object.Instantiate(bulletPrefab, startPos, Quaternion.identity);
        bulletObj.name = $"Bullet_{m_ProjectilesFired + 1}";

        // --- 诊断：检查实例化后子弹的碰撞体和物理层 ---
        var bulletColliders = bulletObj.GetComponentsInChildren<Collider>(true);
        var bulletRigidbody = bulletObj.GetComponent<Rigidbody>();
        string bulletColliderInfo = bulletColliders.Length == 0
            ? "⚠ 无 Collider！碰撞检测将失效"
            : string.Join(", ", System.Array.ConvertAll(bulletColliders, c =>
                $"{c.gameObject.name}.{c.GetType().Name}(trigger={c.isTrigger},enabled={c.enabled})"));
        DebugEx.LogModule("ChangeMagicCircle",
            $"  ├─ [子弹诊断] 实例化完成:\n" +
            $"     ├─ 位置: {bulletObj.transform.position}\n" +
            $"     ├─ Layer: {LayerMask.LayerToName(bulletObj.layer)}({bulletObj.layer})\n" +
            $"     ├─ Colliders: {bulletColliderInfo}\n" +
            $"     ├─ Rigidbody: {(bulletRigidbody != null ? $"✓ isKinematic={bulletRigidbody.isKinematic}" : "✗ 无 Rigidbody")}\n" +
            $"     └─ 施法者 Camp={m_Caster.Camp}");

        // ⭐ 动态添加 ChessProjectile 组件
        var projectile = bulletObj.AddComponent<ChessProjectile>();
        if (projectile == null)
        {
            DebugEx.WarningModule("ChangeMagicCircle", $"  └─ ✗ 添加 ChessProjectile 组件失败");
            Object.Destroy(bulletObj);
            return;
        }

        // 初始化子弹（竖直向下模式）
        Vector3 projectileDirection = m_SpawnPlane != null ? Vector3.down : (targetPos - startPos).normalized;
        DebugEx.LogModule("ChangeMagicCircle",
            $"  ├─ [子弹初始化] camp={m_Caster.Camp}, dir={projectileDirection}, speed={m_Config.ProjectileSpeed}, damage={damage:F1}{(isCritical ? " 暴击" : "")}");

        projectile.Initialize(
            m_Caster.Camp,
            null,  // 无锁定目标
            targetPos,  // 目标位置
            projectileDirection,  // 发射方向（竖直向下）
            (float)m_Config.ProjectileSpeed,
            1,  // 穿透数=1
            (target) => OnProjectileHit(target, damage, isCritical)
        );

        DebugEx.LogModule("ChangeMagicCircle", $"  └─ ✓ 子弹 {m_ProjectilesFired} 已实例化并发射");
    }

    /// <summary>
    /// 子弹命中回调：应用伤害、特效、Buff
    /// </summary>
    private void OnProjectileHit(ChessEntity target, double damage, bool isCritical)
    {
        if (target == null || target.CurrentState == ChessState.Dead)
            return;

        var attr = target.Attribute;
        double hpBefore = attr != null ? attr.CurrentHp : 0;

        // 1. 受击特效
        if (m_Config.HitEffectId > 0)
        {
            CombatVFXManager.PlayEffect(m_Config.HitEffectId, target.transform.position);
        }

        // 2. 造成伤害
        bool isMagic = m_Config.DamageType == 2;
        bool isTrue = m_Config.DamageType == 3;
        attr.TakeDamage(damage, isMagic, isTrue, isCritical);

        // 3. 应用命中 Buff
        EffectExecutor.ApplyBuffsOnHit(m_Config, m_Caster, target);

        DebugEx.LogModule(
            "ChangeMagicCircle",
            $"→ 子弹命中:\n" +
            $"  ├─ 目标: {target.Config?.Name} (camp={target.Camp})\n" +
            $"  ├─ HP: {hpBefore:F1} → {attr.CurrentHp:F1}/{attr.MaxHp:F1}\n" +
            $"  └─ 伤害: {damage:F1}{(isCritical ? " (暴击!)" : "")}, 类型={m_Config.DamageType}"
        );
    }

    /// <summary>
    /// 从 CustomData JSON 解析配置
    /// </summary>
    private CustomDataWrapper ParseCustomData(string customData)
    {
        if (string.IsNullOrEmpty(customData))
        {
            return null;
        }

        try
        {
            var data = JsonUtility.FromJson<CustomDataWrapper>(customData);
            return data;
        }
        catch (System.Exception ex)
        {
            DebugEx.WarningModule("ChangeMagicCircle", $"  ├─ ⚠ 解析 CustomData 失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 法阵效果结束
    /// </summary>
    private void OnComplete()
    {
        DebugEx.LogModule(
            "ChangeMagicCircle",
            $"✓ 法阵效果结束:\n" +
            $"  ├─ 计划发射: {m_Config.HitCount}发\n" +
            $"  ├─ 实际发射: {m_ProjectilesFired}发\n" +
            $"  ├─ 剩余时间: {m_RemainingTime:F2}s\n" +
            $"  └─ 销毁法阵GameObject"
        );

        // 销毁自身
        Destroy(gameObject);
    }

    #endregion

    #region 辅助类

    /// <summary>
    /// CustomData JSON 包装类
    /// 格式: {"ProjectilePrefabId":3007,"SpawnHeight":0.5}
    /// </summary>
    [System.Serializable]
    private class CustomDataWrapper
    {
        /// <summary>子弹预制体资源ID（法阵发射的投射物）</summary>
        public int ProjectilePrefabId;

        /// <summary>已不使用（高度现由生成平面决定）</summary>
        public float SpawnHeight = 5f;
    }

    #endregion
}
