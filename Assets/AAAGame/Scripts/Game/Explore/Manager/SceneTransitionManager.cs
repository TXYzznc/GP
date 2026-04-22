using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 战斗场景转换管理器
/// 统一管理进入/退出战斗时的场景清理，包括：
/// - 玩家位置转换（进入战场上方隔离区）
/// - 敌人的可交互性（不显示/禁用）
/// - 环境物体的溶解过渡
/// - 玩家战斗标记的设置
/// 降低与 CombatPreparationState/CombatState 的耦合
/// </summary>
public class SceneTransitionManager
{
    #region 单例

    private static SceneTransitionManager s_Instance;
    public static SceneTransitionManager Instance => s_Instance ??= new SceneTransitionManager();

    #endregion

    #region 私有字段

    /// <summary>玩家战斗前的位置（用于离开战斗时恢复）</summary>
    private Vector3 m_PlayerPositionBeforeCombat;

    /// <summary>玩家战斗前的旋转（用于离开战斗时恢复）</summary>
    private Quaternion m_PlayerRotationBeforeCombat;

    #endregion

    #region 公共方法

    /// <summary>
    /// 进入战斗准备前的场景准备 - 隐藏敌人、交互物体，记录玩家状态
    /// （应在生成战场前调用）
    /// </summary>
    public void PrepareBeforeArenaSpawn()
    {
        DebugEx.LogModule("SceneTransitionManager", "开始战斗准备...");

        // 1. 记录玩家原始位置和旋转（在生成战场前）
        RecordPlayerStateBeforeCombat();

        // ⭐ 同时记录到 PlayerCharacterManager（用于后续的 RestorePositionAfterCombat）
        if (PlayerCharacterManager.Instance != null)
        {
            PlayerCharacterManager.Instance.RecordPositionBeforeCombat();
            DebugEx.LogModule("SceneTransitionManager", "已通知 PlayerCharacterManager 记录位置");
        }

        // 2. 隐藏敌人（摄像机排除 Enemy Layer）
        HideEnemies();

        // 3. 隐藏交互物体（禁用激活状态）
        HideInteractives();

        // 4. 标记玩家进入战斗（敌人AI停止索敌）
        SetPlayerCombatFlag(true);

        DebugEx.LogModule("SceneTransitionManager", "战斗准备完成");
    }

    /// <summary>
    /// 生成战场后的玩家位置调整和视效
    /// （应在生成战场后调用）
    /// </summary>
    public async UniTask FinalizeAfterArenaSpawn()
    {
        DebugEx.LogModule("SceneTransitionManager", "开始战场最终化...");

        // 1. 将玩家移至 PlayerAnchor（战场已生成）
        MovePlayerToArena();

        // 2. 播放溶解过渡效果（从探索场景显示战斗场地）
        var battleArena = BattleArenaManager.Instance.CurrentArena;
        if (battleArena != null)
        {
            await DissolveTransitionManager.Instance.TransitionToBattle(battleArena);
        }

        DebugEx.LogModule("SceneTransitionManager", "战场最终化完成");
    }

    /// <summary>
    /// 进入战斗准备 - 清理场景（隐藏敌人、隐藏交互物体、溶解环境物体）
    /// 注：玩家位置调整已分离到 PrepareBeforeArenaSpawn 和 FinalizeAfterArenaSpawn
    /// </summary>
    [System.Obsolete("使用 PrepareBeforeArenaSpawn() 和 FinalizeAfterArenaSpawn() 替代")]
    public async UniTask EnterCombatAsync()
    {
        DebugEx.LogModule("SceneTransitionManager", "开始进入战斗场景转换...");

        // 向后兼容：合并两个方法
        PrepareBeforeArenaSpawn();
        await FinalizeAfterArenaSpawn();

        DebugEx.LogModule("SceneTransitionManager", "进入战斗场景转换完成");
    }

    /// <summary>
    /// 离开战斗 - 恢复场景（恢复玩家位置、显示敌人、显示交互物体、溶解显示环境物体）
    /// </summary>
    public async UniTask ExitCombatAsync()
    {
        DebugEx.LogModule("SceneTransitionManager", "开始离开战斗场景转换...");

        // 1. 恢复玩家位置
        RestorePlayerPosition();

        // 2. 清除玩家战斗标记（敌人可重新索敌）
        SetPlayerCombatFlag(false);

        // 3. 显示敌人（摄像机恢复 Enemy Layer）
        ShowEnemies();

        // 4. 显示交互物体（恢复激活状态）
        ShowInteractives();

        // 5. 溶解显示环境物体（异步）
        await DissolveTransitionManager.Instance.TransitionToExploration();

        DebugEx.LogModule("SceneTransitionManager", "离开战斗场景转换完成");
    }

    /// <summary>
    /// 清理管理器（场景切换时调用）
    /// </summary>
    public void Cleanup()
    {
        DebugEx.LogModule("SceneTransitionManager", "已清理");
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 隐藏敌人（摄像机排除 Enemy Layer）
    /// </summary>
    private void HideEnemies()
    {
        var camera = CameraRegistry.ThirdPersonCamera;
        if (camera != null)
        {
            camera.ExcludeLayer(LayerHelper.Layer.Enemy);
            DebugEx.LogModule("SceneTransitionManager", "敌人已隐藏（摄像机排除 Enemy Layer）");
        }
        else
        {
            DebugEx.WarningModule("SceneTransitionManager", "未找到 ThirdPersonCamera");
        }
    }

    /// <summary>
    /// 显示敌人（摄像机恢复 Enemy Layer）
    /// </summary>
    private void ShowEnemies()
    {
        var camera = CameraRegistry.ThirdPersonCamera;
        if (camera != null)
        {
            camera.IncludeLayer(LayerHelper.Layer.Enemy);
            DebugEx.LogModule("SceneTransitionManager", "敌人已显示（摄像机恢复 Enemy Layer）");
        }
        else
        {
            DebugEx.WarningModule("SceneTransitionManager", "未找到 ThirdPersonCamera");
        }
    }

    /// <summary>
    /// 隐藏交互物体（禁用激活状态）
    /// </summary>
    private void HideInteractives()
    {
        int interactiveLayer = LayerMask.NameToLayer("Interactive");
        var allObjects = Object.FindObjectsOfType<GameObject>(true);
        foreach (var obj in allObjects)
        {
            if (obj.layer == interactiveLayer)
            {
                obj.SetActive(false);
            }
        }

        DebugEx.LogModule("SceneTransitionManager", "交互物体已隐藏");
    }

    /// <summary>
    /// 显示交互物体（恢复激活状态）
    /// </summary>
    private void ShowInteractives()
    {
        int interactiveLayer = LayerMask.NameToLayer("Interactive");
        var allObjects = Object.FindObjectsOfType<GameObject>(true);
        foreach (var obj in allObjects)
        {
            if (obj.layer == interactiveLayer && !obj.activeSelf)
            {
                obj.SetActive(true);
            }
        }

        DebugEx.LogModule("SceneTransitionManager", "交互物体已显示");
    }

    /// <summary>
    /// 设置/清除玩家战斗标记
    /// </summary>
    private void SetPlayerCombatFlag(bool isInCombat)
    {
        var playerManager = PlayerCharacterManager.Instance;
        if (playerManager == null)
        {
            DebugEx.WarningModule("SceneTransitionManager", "未找到玩家管理器");
            return;
        }

        var playerGo = playerManager.CurrentPlayerCharacter;
        if (playerGo == null)
        {
            DebugEx.WarningModule("SceneTransitionManager", "未找到玩家角色");
            return;
        }

        var flag = playerGo.GetComponent<PlayerCombatFlag>();
        if (flag == null)
        {
            flag = playerGo.AddComponent<PlayerCombatFlag>();
        }

        flag.IsInCombat = isInCombat;
        DebugEx.LogModule("SceneTransitionManager", $"玩家战斗标记已设置: {isInCombat}");
    }

    /// <summary>
    /// 在玩家进入战斗前，记录其位置和旋转
    /// （此方法应在战场生成之前调用）
    /// </summary>
    private void RecordPlayerStateBeforeCombat()
    {
        var playerManager = PlayerCharacterManager.Instance;
        if (playerManager == null)
        {
            DebugEx.WarningModule("SceneTransitionManager", "未找到玩家管理器");
            return;
        }

        var playerGo = playerManager.CurrentPlayerCharacter;
        if (playerGo == null)
        {
            DebugEx.WarningModule("SceneTransitionManager", "未找到玩家角色");
            return;
        }

        m_PlayerPositionBeforeCombat = playerGo.transform.position;
        m_PlayerRotationBeforeCombat = playerGo.transform.rotation;

        DebugEx.LogModule("SceneTransitionManager",
            $"记录玩家战斗前状态 - 位置: {m_PlayerPositionBeforeCombat}, 旋转: {m_PlayerRotationBeforeCombat.eulerAngles}");
    }

    /// <summary>
    /// 将玩家移至 PlayerAnchor（战场生成后调用）
    /// </summary>
    private void MovePlayerToArena()
    {
        var playerManager = PlayerCharacterManager.Instance;
        if (playerManager == null)
        {
            DebugEx.WarningModule("SceneTransitionManager", "未找到玩家管理器");
            return;
        }

        var playerGo = playerManager.CurrentPlayerCharacter;
        if (playerGo == null)
        {
            DebugEx.WarningModule("SceneTransitionManager", "未找到玩家角色");
            return;
        }

        var battleArena = BattleArenaManager.Instance.CurrentArena;
        if (battleArena == null)
        {
            DebugEx.WarningModule("SceneTransitionManager", "战斗场地不存在，无法移动玩家");
            return;
        }

        var playerAnchor = battleArena.transform.Find("PlayerAnchor");
        if (playerAnchor == null)
        {
            DebugEx.WarningModule("SceneTransitionManager", "战斗场地中未找到 PlayerAnchor");
            return;
        }

        // 获取玩家当前底部位置
        Vector3 playerBottomPos = EntityPositionHelper.GetBottomPosition(playerGo);

        // 计算移动偏移：目标锚点 - 当前底部位置
        Vector3 offset = playerAnchor.position - playerBottomPos;

        // 使用 PlayerController 的 TeleportTo 正确处理 CharacterController
        PlayerController controller = playerGo.GetComponent<PlayerController>();
        if (controller != null)
        {
            // 计算目标朝向（指向 PlayerAnchor 方向，保持战场朝向）
            Vector3 targetForward = playerAnchor.forward;
            controller.TeleportTo(playerAnchor.position, targetForward);
            DebugEx.LogModule("SceneTransitionManager",
                $"玩家已通过 TeleportTo 移至战场 (PlayerAnchor: {playerAnchor.position}, 朝向: {playerAnchor.eulerAngles})");
        }
        else
        {
            // 降级方案：直接设置位置
            playerGo.transform.position += offset;
            DebugEx.LogModule("SceneTransitionManager",
                $"玩家已移至战场 (玩家底部对齐 PlayerAnchor: {playerAnchor.position})");
        }
    }

    /// <summary>
    /// 恢复玩家到战斗前的位置
    /// </summary>
    private void RestorePlayerPosition()
    {
        var playerManager = PlayerCharacterManager.Instance;
        if (playerManager == null)
        {
            DebugEx.WarningModule("SceneTransitionManager", "未找到玩家管理器");
            return;
        }

        var playerGo = playerManager.CurrentPlayerCharacter;
        if (playerGo == null)
        {
            DebugEx.WarningModule("SceneTransitionManager", "未找到玩家角色");
            return;
        }

        // 使用 PlayerController 的 TeleportTo 确保 CharacterController 正确处理
        PlayerController controller = playerGo.GetComponent<PlayerController>();
        if (controller != null)
        {
            // 计算朝向向量
            Vector3 forward = m_PlayerRotationBeforeCombat * Vector3.forward;
            controller.TeleportTo(m_PlayerPositionBeforeCombat, forward);
            DebugEx.LogModule("SceneTransitionManager",
                $"玩家已通过 TeleportTo 恢复位置 (移至 {m_PlayerPositionBeforeCombat}, 朝向: {m_PlayerRotationBeforeCombat.eulerAngles})");
        }
        else
        {
            // 降级方案：直接设置位置
            playerGo.transform.position = m_PlayerPositionBeforeCombat;
            playerGo.transform.rotation = m_PlayerRotationBeforeCombat;
            DebugEx.LogModule("SceneTransitionManager",
                $"玩家已恢复位置 (移至 {m_PlayerPositionBeforeCombat})");
        }
    }

    #endregion
}
