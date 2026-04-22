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

    #endregion

    #region 公共方法

    /// <summary>
    /// 进入战斗准备 - 清理场景（移动玩家到战场、隐藏敌人、隐藏交互物体、溶解环境物体）
    /// </summary>
    public async UniTask EnterCombatAsync()
    {
        DebugEx.LogModule("SceneTransitionManager", "开始进入战斗场景转换...");

        // 1. 记录玩家原始位置并移至战场
        MovePlayerToArena();

        // 2. 隐藏敌人（摄像机排除 Enemy Layer）
        HideEnemies();

        // 3. 隐藏交互物体（禁用激活状态）
        HideInteractives();

        // 4. 标记玩家进入战斗（敌人AI停止索敌）
        SetPlayerCombatFlag(true);

        // 5. 溶解隐藏环境物体（异步）
        var battleArena = BattleArenaManager.Instance.CurrentArena;
        await DissolveTransitionManager.Instance.TransitionToBattle(battleArena);

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
    /// 将玩家移至战场（玩家底部对齐 PlayerAnchor 位置）
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
            DebugEx.WarningModule("SceneTransitionManager", "战斗场地不存在");
            return;
        }

        var playerAnchor = battleArena.transform.Find("PlayerAnchor");
        if (playerAnchor == null)
        {
            DebugEx.WarningModule("SceneTransitionManager", "战斗场地中未找到 PlayerAnchor");
            return;
        }

        // 记录原始位置
        m_PlayerPositionBeforeCombat = playerGo.transform.position;

        // 获取玩家当前底部位置
        Vector3 playerBottomPos = EntityPositionHelper.GetBottomPosition(playerGo);

        // 计算移动偏移：目标锚点 - 当前底部位置
        Vector3 offset = playerAnchor.position - playerBottomPos;

        // 移动玩家
        playerGo.transform.position += offset;
        DebugEx.LogModule("SceneTransitionManager", $"玩家已移至战场 (玩家底部对齐 PlayerAnchor: {playerAnchor.position})");
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

        playerGo.transform.position = m_PlayerPositionBeforeCombat;
        DebugEx.LogModule("SceneTransitionManager", $"玩家已恢复位置 (移至 {m_PlayerPositionBeforeCombat})");
    }

    #endregion
}
