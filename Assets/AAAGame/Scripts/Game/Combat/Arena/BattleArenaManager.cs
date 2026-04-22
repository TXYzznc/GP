using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityGameFramework.Runtime;

/// <summary>
/// 战斗场地管理器
/// 负责战斗场地的生成和销毁，以及区域管理
/// </summary>
public class BattleArenaManager
{
    #region 单例

    private static BattleArenaManager s_Instance;
    public static BattleArenaManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new BattleArenaManager();
            }
            return s_Instance;
        }
    }

    private BattleArenaManager() { }

    #endregion

    #region 字段

    /// <summary>当前战斗场地实例</summary>
    private GameObject m_CurrentArena;

    /// <summary>战斗场地预制体ResourceId</summary>
    private int m_ArenaResourceId = -1;

    /// <summary>玩家锚点（场地中的标记点）</summary>
    private Transform m_PlayerAnchor;

    /// <summary>我方区域 Collider</summary>
    private Collider m_PlayerZoneCollider;

    /// <summary>敌方区域 Collider</summary>
    private Collider m_EnemyZoneCollider;

    /// <summary>战场 Y 轴偏移高度（隔离周边敌人）</summary>
    private const float ARENA_HEIGHT_OFFSET = 20f;

    #endregion

    #region 属性

    /// <summary>获取当前战斗场地实例</summary>
    public GameObject CurrentArena => m_CurrentArena;

    #endregion

    #region 公共接口

    /// <summary>
    /// 初始化管理器
    /// </summary>
    /// <param name="arenaResourceId">战斗场地预制体ResourceId</param>
    public void Initialize(int arenaResourceId = 0)
    {
        m_ArenaResourceId = arenaResourceId;
        DebugEx.LogModule("BattleArenaManager", $"初始化完成 (ResourceId={arenaResourceId})");
    }

    /// <summary>
    /// 异步生成战斗场地（对齐玩家底部位置和朝向）
    /// </summary>
    /// <param name="playerTransform">玩家 Transform</param>
    public async UniTask<GameObject> SpawnArenaAsync(Transform playerTransform)
    {
        if (playerTransform == null)
        {
            DebugEx.ErrorModule("BattleArenaManager", "玩家 Transform 为空");
            return null;
        }

        // 获取玩家底部位置
        Vector3 playerBottomPosition = GetPlayerBottomPosition(playerTransform);

        // 获取玩家朝向（仅保留 Y 轴旋转）
        Quaternion playerRotation = Quaternion.Euler(0, playerTransform.eulerAngles.y, 0);

        // 如果已有场地,先销毁
        if (m_CurrentArena != null)
        {
            DestroyArena();
        }

        // 加载预制体
        GameObject prefab = null;
        if (m_ArenaResourceId > 0)
        {
            prefab = await GameExtension.ResourceExtension.LoadPrefabAsync(m_ArenaResourceId);
        }

        if (prefab == null)
        {
            DebugEx.WarningModule("BattleArenaManager", "战斗场地预制体加载失败,创建默认场地");
            m_CurrentArena = CreateDefaultArena(playerBottomPosition, playerRotation);
        }
        else
        {
            // ⭐ 计算战场的正确旋转，使得子对象 PlayerAnchor 的绝对方向与玩家朝向一致
            Quaternion arenaRotation = CalculateArenaRotation(prefab, playerRotation);

            // 计算场地生成位置（让 PlayerAnchor 对齐玩家底部位置，考虑旋转）
            Vector3 spawnPosition = CalculateArenaSpawnPosition(prefab, playerBottomPosition, arenaRotation);
            // 向上偏移战场，物理隔离周边敌人
            spawnPosition.y += ARENA_HEIGHT_OFFSET;
            m_CurrentArena = Object.Instantiate(prefab, spawnPosition, arenaRotation);
            m_CurrentArena.name = "BattleArena";
        }

        // 缓存区域引用
        CacheZoneReferences();

        DebugEx.LogModule("BattleArenaManager",
            $"战斗场地已生成，PlayerAnchor 对齐玩家底部位置: {playerBottomPosition}, 战场朝向: {CalculateArenaRotation(prefab, playerRotation).eulerAngles.y}°, 玩家朝向: {playerRotation.eulerAngles.y}°");

        return m_CurrentArena;
    }

    /// <summary>
    /// 销毁战斗场地
    /// </summary>
    public void DestroyArena()
    {
        if (m_CurrentArena != null)
        {
            Object.Destroy(m_CurrentArena);
            m_CurrentArena = null;
            m_PlayerAnchor = null;
            m_PlayerZoneCollider = null;
            m_EnemyZoneCollider = null;
            DebugEx.LogModule("BattleArenaManager", "战斗场地已销毁");
        }
    }

    /// <summary>
    /// 清理管理器
    /// </summary>
    public void Cleanup()
    {
        DestroyArena();
        m_ArenaResourceId = 0;
        DebugEx.LogModule("BattleArenaManager", "已清理");
    }

    #endregion

    #region 区域查询接口

    /// <summary>
    /// 检查世界坐标点是否在我方区域
    /// </summary>
    public bool IsInPlayerZone(Vector3 worldPosition)
    {
        if (m_PlayerZoneCollider == null)
        {
            DebugEx.WarningModule("BattleArenaManager", "PlayerZone Collider 未初始化，默认返回 true");
            return true;
        }

        // 用 ClosestPoint 精确判断点是否在 Collider 几何体内部
        // 若点在内部，ClosestPoint 返回该点本身（距离为 0）
        return m_PlayerZoneCollider.ClosestPoint(worldPosition) == worldPosition;
    }

    /// <summary>
    /// 检查世界坐标点是否在敌方区域
    /// </summary>
    public bool IsInEnemyZone(Vector3 worldPosition)
    {
        if (m_EnemyZoneCollider == null)
        {
            DebugEx.WarningModule("BattleArenaManager", "EnemyZone Collider 未初始化，默认返回 false");
            return false;
        }

        return m_EnemyZoneCollider.ClosestPoint(worldPosition) == worldPosition;
    }

    /// <summary>
    /// 获取我方区域边界（世界坐标）
    /// </summary>
    public Bounds GetPlayerZoneBounds()
    {
        if (m_PlayerZoneCollider == null)
        {
            DebugEx.WarningModule("BattleArenaManager", "PlayerZone Collider 未初始化，返回默认 Bounds");
            return new Bounds(Vector3.zero, Vector3.one * 10);
        }

        return m_PlayerZoneCollider.bounds;
    }

    /// <summary>
    /// 获取敌方区域边界（世界坐标）
    /// </summary>
    public Bounds GetEnemyZoneBounds()
    {
        if (m_EnemyZoneCollider == null)
        {
            DebugEx.WarningModule("BattleArenaManager", "EnemyZone Collider 未初始化，返回默认 Bounds");
            return new Bounds(Vector3.zero, Vector3.one * 10);
        }

        return m_EnemyZoneCollider.bounds;
    }

    /// <summary>
    /// 获取敌方区域中心点（世界坐标）
    /// </summary>
    public Vector3 GetEnemyZoneCenter()
    {
        if (m_EnemyZoneCollider == null)
        {
            DebugEx.WarningModule("BattleArenaManager", "EnemyZone Collider 未初始化，返回默认中心点");
            return Vector3.forward * 5;
        }

        return m_EnemyZoneCollider.bounds.center;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 获取玩家底部位置
    /// </summary>
    private Vector3 GetPlayerBottomPosition(Transform playerTransform)
    {
        // 尝试获取 CharacterController
        CharacterController characterController = playerTransform.GetComponent<CharacterController>();
        if (characterController != null)
        {
            Vector3 bottomPos = playerTransform.position - new Vector3(0, characterController.height / 2f, 0);
            DebugEx.LogModule("BattleArenaManager",
                $"通过 CharacterController 获取底部位置: {bottomPos} (高度={characterController.height})");
            return bottomPos;
        }

        // 尝试获取 Collider
        Collider collider = playerTransform.GetComponent<Collider>();
        if (collider != null)
        {
            Vector3 bottomPos = new Vector3(
                playerTransform.position.x,
                collider.bounds.min.y,
                playerTransform.position.z
            );
            DebugEx.LogModule("BattleArenaManager",
                $"通过 Collider 获取底部位置: {bottomPos}");
            return bottomPos;
        }

        // 如果都没有，使用 Transform 位置并警告
        DebugEx.WarningModule("BattleArenaManager",
            "玩家对象没有 CharacterController 或 Collider，使用 Transform 位置作为底部位置");
        return playerTransform.position;
    }

    /// <summary>
    /// 计算战场的正确旋转，使得生成后 PlayerAnchor 的绝对朝向与玩家朝向一致
    ///
    /// 旋转场景说明：
    ///   1. 预制体中 PlayerAnchor 相对战场有一个本地朝向（如朝东 90°）
    ///   2. 通过旋转战场，使 PlayerAnchor 最终的绝对朝向与玩家朝向一致（如玩家朝南 180°）
    ///   3. PlayerAnchor 相对战场的朝向不变（仍是 90°），但战场本身旋转了
    ///
    /// 数学推导：
    ///   PlayerAnchor 绝对朝向 = 战场朝向 + PlayerAnchor 相对朝向
    ///   目标：PlayerAnchor 绝对朝向 = 玩家朝向
    ///   所以：战场朝向 + PlayerAnchor 相对朝向 = 玩家朝向
    ///   即：arenaRotation × playerAnchorLocalRotation = playerRotation
    ///   解得：arenaRotation = playerRotation × (playerAnchorLocalRotation)^-1
    ///
    /// 具体例子：
    ///   - 玩家朝向：南 180°
    ///   - PlayerAnchor 本地朝向：东 90°
    ///   - 计算：战场朝向 = 180° × (90°)^-1 = 180° × 270° = 90° （朝东）
    ///   - 验证：东 90° + 东 90° = 南 180° ✓
    /// </summary>
    private Quaternion CalculateArenaRotation(GameObject arenaPrefab, Quaternion playerRotation)
    {
        // 查找预制体中的 PlayerAnchor
        Transform playerAnchor = arenaPrefab.transform.Find("PlayerAnchor");

        if (playerAnchor == null)
        {
            DebugEx.WarningModule("BattleArenaManager",
                "预制体中未找到 PlayerAnchor，使用玩家旋转作为战场旋转");
            return playerRotation;
        }

        // PlayerAnchor 在预制体中的本地朝向（预制体的默认旋转通常是 Quaternion.identity）
        Quaternion playerAnchorLocalRotation = playerAnchor.localRotation;

        // 计算战场旋转：使得旋转后的 PlayerAnchor 朝向与玩家朝向一致
        // arenaRotation * playerAnchorLocalRotation = playerRotation
        // arenaRotation = playerRotation * playerAnchorLocalRotation^-1
        Quaternion arenaRotation = playerRotation * Quaternion.Inverse(playerAnchorLocalRotation);

        DebugEx.LogModule("BattleArenaManager",
            $"战场旋转计算: 玩家朝向={playerRotation.eulerAngles.y}°, " +
            $"PlayerAnchor本地朝向={playerAnchorLocalRotation.eulerAngles.y}°, " +
            $"计算后战场朝向={arenaRotation.eulerAngles.y}°");

        return arenaRotation;
    }

    /// <summary>
    /// 计算场地生成位置（让 PlayerAnchor 对齐玩家底部位置，考虑旋转）
    /// </summary>
    private Vector3 CalculateArenaSpawnPosition(GameObject arenaPrefab, Vector3 playerBottomPosition, Quaternion arenaRotation)
    {
        // 查找预制体中的 PlayerAnchor
        Transform playerAnchor = arenaPrefab.transform.Find("PlayerAnchor");

        if (playerAnchor == null)
        {
            DebugEx.WarningModule("BattleArenaManager",
                "预制体中未找到 PlayerAnchor，使用默认偏移");
            return playerBottomPosition;
        }

        // 计算偏移：考虑旋转后的锚点位置
        // 场地位置 = 玩家底部位置 - (战场旋转后的锚点本地坐标)
        Vector3 anchorLocalPos = playerAnchor.localPosition;
        Vector3 rotatedOffset = arenaRotation * anchorLocalPos;
        Vector3 spawnPosition = playerBottomPosition - rotatedOffset;

        DebugEx.LogModule("BattleArenaManager",
            $"场地生成位置计算: 玩家底部位置={playerBottomPosition}, " +
            $"锚点本地坐标={anchorLocalPos}, 战场旋转后偏移={rotatedOffset}, 场地位置={spawnPosition}");

        return spawnPosition;
    }

    /// <summary>
    /// 缓存区域引用
    /// </summary>
    private void CacheZoneReferences()
    {
        if (m_CurrentArena == null)
        {
            DebugEx.ErrorModule("BattleArenaManager", "当前场地为空，无法缓存区域引用");
            return;
        }

        // 缓存 PlayerAnchor
        m_PlayerAnchor = m_CurrentArena.transform.Find("PlayerAnchor");
        if (m_PlayerAnchor == null)
        {
            DebugEx.WarningModule("BattleArenaManager", "未找到 PlayerAnchor");
        }

        // 缓存 PlayerZone Collider
        Transform playerZone = m_CurrentArena.transform.Find("PlayerZone");
        if (playerZone != null)
        {
            m_PlayerZoneCollider = playerZone.GetComponent<Collider>();
            if (m_PlayerZoneCollider != null)
            {
                DebugEx.LogModule("BattleArenaManager",
                    $"PlayerZone Collider 已缓存，边界={m_PlayerZoneCollider.bounds}");
            }
            else
            {
                DebugEx.WarningModule("BattleArenaManager", "PlayerZone 没有 Collider 组件");
            }
        }
        else
        {
            DebugEx.WarningModule("BattleArenaManager", "未找到 PlayerZone");
        }

        // 缓存 EnemyZone Collider
        Transform enemyZone = m_CurrentArena.transform.Find("EnemyZone");
        if (enemyZone != null)
        {
            m_EnemyZoneCollider = enemyZone.GetComponent<Collider>();
            if (m_EnemyZoneCollider != null)
            {
                DebugEx.LogModule("BattleArenaManager",
                    $"EnemyZone Collider 已缓存，边界={m_EnemyZoneCollider.bounds}");
            }
            else
            {
                DebugEx.WarningModule("BattleArenaManager", "EnemyZone 没有 Collider 组件");
            }
        }
        else
        {
            DebugEx.WarningModule("BattleArenaManager", "未找到 EnemyZone");
        }
    }

    /// <summary>
    /// 创建默认战斗场地(用于测试)
    /// </summary>
    private GameObject CreateDefaultArena(Vector3 position, Quaternion playerRotation)
    {
        GameObject arena = new GameObject("BattleArena_Default");
        arena.transform.position = position;
        // ⭐ 对于默认场地，PlayerAnchor 默认朝向是 (0, 0, 0)，所以战场旋转就是玩家旋转
        arena.transform.rotation = playerRotation;

        // 创建 PlayerAnchor
        GameObject playerAnchor = new GameObject("PlayerAnchor");
        playerAnchor.transform.SetParent(arena.transform);
        playerAnchor.transform.localPosition = Vector3.zero;
        playerAnchor.transform.localRotation = Quaternion.identity;

        // 创建我方区域
        GameObject playerZone = GameObject.CreatePrimitive(PrimitiveType.Plane);
        playerZone.name = "PlayerZone";
        playerZone.transform.SetParent(arena.transform);
        playerZone.transform.localPosition = new Vector3(0, 0, -5);
        playerZone.transform.localScale = new Vector3(2, 1, 2);
        playerZone.layer = LayerMask.NameToLayer("PlacementPlane");

        // 确保有 Collider（Plane 默认有 MeshCollider）
        Collider playerCollider = playerZone.GetComponent<Collider>();
        if (playerCollider != null)
        {
            playerCollider.isTrigger = true; // 设置为 Trigger
        }

        // 创建敌方区域
        GameObject enemyZone = GameObject.CreatePrimitive(PrimitiveType.Plane);
        enemyZone.name = "EnemyZone";
        enemyZone.transform.SetParent(arena.transform);
        enemyZone.transform.localPosition = new Vector3(0, 0, 5);
        enemyZone.transform.localScale = new Vector3(2, 1, 2);

        // 确保有 Collider
        Collider enemyCollider = enemyZone.GetComponent<Collider>();
        if (enemyCollider != null)
        {
            enemyCollider.isTrigger = true; // 设置为 Trigger
        }

        DebugEx.WarningModule("BattleArenaManager", "使用默认战斗场地(仅用于测试)");
        return arena;
    }

    #endregion
}
