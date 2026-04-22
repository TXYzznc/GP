using System;
using GameExtension;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 玩家角色管理器 - 负责玩家角色的生成、销毁和管理
/// </summary>
public class PlayerCharacterManager : SingletonBase<PlayerCharacterManager>
{
    #region 属性

    /// <summary>
    /// 当前玩家角色实例
    /// </summary>
    public GameObject CurrentPlayerCharacter { get; private set; }

    /// <summary>
    /// 当前摄像机装备
    /// </summary>
    public GameObject CurrentCameraRig { get; private set; }

    /// <summary>
    /// 角色是否已生成
    /// </summary>
    public bool IsCharacterSpawned => CurrentPlayerCharacter != null;

    #endregion

    #region 字段

    // ⭐ 战斗前的临时位置记录（新增）
    private Vector3 m_PositionBeforeCombat;
    private Quaternion m_RotationBeforeCombat;
    private bool m_HasRecordedPosition;

    #endregion

    #region Unity 生命周期

    protected override void Awake()
    {
        base.Awake();
    }

    #endregion

    /// <summary>
    /// 在场景加载完成后生成玩家角色
    /// ⭐ 玩家生成位置从 SceneTable.DefaultSpawnPosId → PosTable 读取
    /// </summary>
    /// <param name="onComplete">生成完成回调</param>
    public void SpawnPlayerCharacterFromSave(Action<GameObject> onComplete = null)
    {
        DebugEx.LogModule("PlayerCharacterManager", "========== 开始生成玩家角色流程 ==========");

        // 获取当前存档数据（只需要召唤师ID）
        var saveData = PlayerAccountDataManager.Instance.CurrentSaveData;
        if (saveData == null)
        {
            Log.Error("[PlayerCharacterManager] ❌ 当前没有存档数据，无法生成角色");
            onComplete?.Invoke(null);
            return;
        }

        int summonerId = saveData.CurrentSummonerId;

        // ⭐ 从当前场景的配置表读取默认出生点
        Vector3 spawnPosition = GetDefaultSpawnPositionForCurrentScene();
        if (spawnPosition == Vector3.zero)
        {
            Log.Warning("[PlayerCharacterManager] ⚠️ 无法读取默认出生点，使用原点 (0, 0, 0)");
        }

        Log.Info(
            $"[PlayerCharacterManager] 准备生成玩家角色: 召唤师ID={summonerId}, 位置={spawnPosition}"
        );

        // 获取召唤师配置表
        var dtSummoner = GF.DataTable.GetDataTable<SummonerTable>();
        var summonerRow = dtSummoner.GetDataRow(summonerId);
        if (summonerRow == null)
        {
            Log.Error($"无法找到召唤师配置: ID={summonerId}");
            onComplete?.Invoke(null);
            return;
        }

        // 计算预制体配置ID
        int prefabConfigId = summonerRow.PrefabId;

        // 异步加载并生成角色
        SpawnCharacter(prefabConfigId, spawnPosition, onComplete);
    }

    /// <summary>
    /// 从当前场景的配置表读取默认出生点坐标
    /// </summary>
    private Vector3 GetDefaultSpawnPositionForCurrentScene()
    {
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        DebugEx.LogModule("PlayerCharacterManager", $"查询场景 '{currentSceneName}' 的默认出生点");

        // 从 SceneTable 获取当前场景配置
        var sceneTable = GF.DataTable.GetDataTable<SceneTable>();
        if (sceneTable == null)
        {
            Log.Error("PlayerCharacterManager: SceneTable 未加载");
            return Vector3.zero;
        }

        var sceneRow = sceneTable.GetDataRow(row => row.SceneName == currentSceneName);
        if (sceneRow == null)
        {
            Log.Warning($"PlayerCharacterManager: 未找到场景 '{currentSceneName}' 的配置");
            return Vector3.zero;
        }

        int defaultSpawnPosId = sceneRow.DefaultSpawnPosId;
        DebugEx.LogModule("PlayerCharacterManager", $"场景默认出生点ID: {defaultSpawnPosId}");

        // 从 PosTable 获取坐标
        var posTable = GF.DataTable.GetDataTable<PosTable>();
        if (posTable == null)
        {
            Log.Error("PlayerCharacterManager: PosTable 未加载");
            return Vector3.zero;
        }

        var posRow = posTable.GetDataRow(defaultSpawnPosId);
        if (posRow == null)
        {
            Log.Warning($"PlayerCharacterManager: 未找到出生点 ID {defaultSpawnPosId} 的配置");
            return Vector3.zero;
        }

        Vector3 spawnPos = posRow.Position;
        DebugEx.LogModule("PlayerCharacterManager",
            $"✅ 读取出生点: ID={defaultSpawnPosId}, 位置={spawnPos}, 描述={posRow.Description}");

        return spawnPos;
    }

    /// <summary>
    /// 生成角色
    /// </summary>
    private async void SpawnCharacter(
        int prefabConfigId,
        Vector3 position,
        Action<GameObject> onComplete
    )
    {
        try
        {
            DebugEx.LogModule("PlayerCharacterManager", "===== SpawnCharacter 开始 =====");

            // 清理旧角色
            if (CurrentPlayerCharacter != null)
            {
                DebugEx.LogModule(
                    "PlayerCharacterManager",
                    $"清理旧角色: {CurrentPlayerCharacter.name}"
                );
                Destroy(CurrentPlayerCharacter);
                CurrentPlayerCharacter = null;
            }

            Log.Info($"[PlayerCharacterManager] 开始加载角色预制体: ConfigId={prefabConfigId}");

            // 使用 ResourceExtension 加载预制体
            var prefabAsset = await ResourceExtension.LoadPrefabAsync(prefabConfigId);

            if (prefabAsset == null)
            {
                Log.Error(
                    $"[PlayerCharacterManager] ❌ 加载角色预制体失败: ConfigId={prefabConfigId}"
                );
                onComplete?.Invoke(null);
                return;
            }

            DebugEx.LogModule(
                "PlayerCharacterManager",
                $"✅ 角色预制体加载成功: {prefabAsset.name}"
            );

            // 实例化角色
            CurrentPlayerCharacter = Instantiate(prefabAsset, position, Quaternion.identity);
            CurrentPlayerCharacter.name = "PlayerCharacter";

            DebugEx.LogModule(
                "PlayerCharacterManager",
                $"✅ 角色实例化成功: {CurrentPlayerCharacter.name}, 位置={position}"
            );

            // 获取或添加角色控制脚本
            PlayerController controller = CurrentPlayerCharacter.GetComponent<PlayerController>();
            if (controller == null)
            {
                DebugEx.LogModule("PlayerCharacterManager", "未找到 PlayerController，正在添加...");
                controller = CurrentPlayerCharacter.AddComponent<PlayerController>();
                Log.Info("[PlayerCharacterManager] ✅ 已添加 PlayerController 组件");
            }
            else
            {
                DebugEx.LogModule("PlayerCharacterManager", "✅ 找到现有的 PlayerController 组件");
            }

            // 激活角色控制器（从存档生成的角色需要启用控制）
            controller.enabled = true;
            DebugEx.LogModule(
                "PlayerCharacterManager",
                $"✅ PlayerController 已激活: enabled={controller.enabled}"
            );

            // 确保战后隐身组件存在
            if (CurrentPlayerCharacter.GetComponent<PostCombatStealth>() == null)
                CurrentPlayerCharacter.AddComponent<PostCombatStealth>();

            // 确保召唤师战斗代理组件存在
            if (CurrentPlayerCharacter.GetComponent<SummonerCombatProxy>() == null)
                CurrentPlayerCharacter.AddComponent<SummonerCombatProxy>();

            // 创建并配置摄像机装备
            DebugEx.LogModule("PlayerCharacterManager", "开始配置摄像机装备...");
            SetupCameraRig(CurrentPlayerCharacter);

            Log.Info($"[PlayerCharacterManager] ✅ 角色生成成功: 位置={position}");
            DebugEx.LogModule("PlayerCharacterManager", "========== 角色生成流程完成 ==========");

            // 调用完成回调
            onComplete?.Invoke(CurrentPlayerCharacter);
        }
        catch (Exception e)
        {
            Log.Error(
                $"[PlayerCharacterManager] ❌ 生成角色时发生错误: {e.Message}\n{e.StackTrace}"
            );
            onComplete?.Invoke(null);
        }
    }

    /// <summary>
    /// 销毁当前角色
    /// </summary>
    public void DestroyCurrentCharacter()
    {
        if (CurrentPlayerCharacter != null)
        {
            Destroy(CurrentPlayerCharacter);
            CurrentPlayerCharacter = null;
            Log.Info("玩家角色已销毁");
        }

        // 销毁摄像机装备
        if (CurrentCameraRig != null)
        {
            // ⭐ 先注销摄像机
            CameraRegistry.UnregisterPlayerCamera();

            Destroy(CurrentCameraRig);
            CurrentCameraRig = null;
            Log.Info("摄像机装备已销毁");
        }
    }

    /// <summary>
    /// 设置摄像机装备
    /// </summary>
    private void SetupCameraRig(GameObject character)
    {
        DebugEx.LogModule("PlayerCharacterManager", "===== SetupCameraRig 开始 =====");

        if (character == null)
        {
            DebugEx.ErrorModule("PlayerCharacterManager", "❌ 角色对象为空，无法配置摄像机");
            return;
        }

        DebugEx.LogModule("PlayerCharacterManager", $"为角色配置摄像机: {character.name}");

        // 清理旧的摄像机装备
        if (CurrentCameraRig != null)
        {
            DebugEx.LogModule(
                "PlayerCharacterManager",
                $"清理旧的摄像机装备: {CurrentCameraRig.name}"
            );
            Destroy(CurrentCameraRig);
        }

        // 创建摄像机装备
        CurrentCameraRig = new("ThirdPersonCamera");
        DebugEx.LogModule("PlayerCharacterManager", $"✅ 创建摄像机装备: {CurrentCameraRig.name}");

        // 创建摄像机对象（作为子对象）
        GameObject cameraObj = new("Camera");
        cameraObj.transform.SetParent(CurrentCameraRig.transform);
        cameraObj.transform.localPosition = Vector3.zero;
        DebugEx.LogModule("PlayerCharacterManager", $"✅ 创建摄像机对象: {cameraObj.name}");

        Camera camera = cameraObj.AddComponent<Camera>();
        camera.tag = "MainCamera";
        DebugEx.LogModule(
            "PlayerCharacterManager",
            $"✅ 添加 Camera 组件: Tag={camera.tag}, Active={camera.gameObject.activeInHierarchy}, Enabled={camera.enabled}"
        );

        // 添加新的 ThirdPersonCamera 组件
        ThirdPersonCamera cameraRig = CurrentCameraRig.AddComponent<ThirdPersonCamera>();
        cameraRig.SetTarget(character.transform);
        DebugEx.LogModule(
            "PlayerCharacterManager",
            $"✅ 添加 ThirdPersonCamera 组件并设置目标: {character.name}"
        );

        // ⭐ 注册到摄像机注册表
        DebugEx.LogModule("PlayerCharacterManager", "开始注册摄像机到 CameraRegistry...");
        CameraRegistry.RegisterPlayerCamera(camera, cameraRig);

        // ✅ 验证注册结果
        if (CameraRegistry.HasPlayerCamera)
        {
            DebugEx.LogModule("PlayerCharacterManager", "✅ 摄像机注册验证成功");
        }
        else
        {
            DebugEx.ErrorModule("PlayerCharacterManager", "❌ 摄像机注册验证失败！");
        }

        // 获取角色控制器并设置摄像机引用
        PlayerController controller = character.GetComponent<PlayerController>();
        if (controller != null)
        {
            DebugEx.LogModule("PlayerCharacterManager", $"找到 PlayerController，设置摄像机引用");
            controller.SetCameraRig(cameraRig);
            DebugEx.LogModule("PlayerCharacterManager", "✅ PlayerController 摄像机引用已设置");
        }
        else
        {
            DebugEx.WarningModule("PlayerCharacterManager", "⚠️ 未找到 PlayerController 组件");
        }

        // ⭐ 优化：使用静态缓存管理 AudioListener，避免 FindObjectsOfType
        DebugEx.LogModule("PlayerCharacterManager", "配置 AudioListener...");

        // 添加新的音频监听器
        if (cameraObj.GetComponent<AudioListener>() == null)
        {
            cameraObj.AddComponent<AudioListener>();
            DebugEx.LogModule("PlayerCharacterManager", "✅ 添加 AudioListener 组件");
        }
        else
        {
            DebugEx.LogModule("PlayerCharacterManager", "AudioListener 已存在");
        }

        Log.Info("[PlayerCharacterManager] ✅ 新版摄像机装备已创建并配置完成");
        DebugEx.LogModule("PlayerCharacterManager", "========== SetupCameraRig 完成 ==========");
    }

    /// <summary>
    /// ⭐ 已弃用：玩家位置现在通过配置表的 DefaultSpawnPosId 来管理
    /// 不再使用存档中的 PlayerPos 字段
    /// </summary>
    [System.Obsolete("玩家位置现在从 SceneTable.DefaultSpawnPosId 读取，不再保存到存档")]
    public void SaveCurrentPosition()
    {
        Log.Warning("SaveCurrentPosition() 已弃用，玩家位置由配置表管理");
    }

    #region 战斗位置管理

    /// <summary>
    /// 记录当前玩家位置（战斗前调用）
    /// </summary>
    public void RecordPositionBeforeCombat()
    {
        if (CurrentPlayerCharacter == null)
        {
            DebugEx.WarningModule("PlayerCharacterManager", "无法记录位置：玩家角色不存在");
            return;
        }

        m_PositionBeforeCombat = CurrentPlayerCharacter.transform.position;
        m_RotationBeforeCombat = CurrentPlayerCharacter.transform.rotation;
        m_HasRecordedPosition = true;

        DebugEx.LogModule(
            "PlayerCharacterManager",
            $"记录战斗前位置: Pos={m_PositionBeforeCombat}, Rot={m_RotationBeforeCombat.eulerAngles}"
        );
    }

    /// <summary>
    /// 恢复玩家到战斗前的位置（战斗后调用）
    /// </summary>
    public void RestorePositionAfterCombat()
    {
        if (!m_HasRecordedPosition)
        {
            DebugEx.WarningModule("PlayerCharacterManager", "没有记录的位置，跳过恢复");
            return;
        }

        if (CurrentPlayerCharacter == null)
        {
            DebugEx.WarningModule("PlayerCharacterManager", "无法恢复位置：玩家角色不存在");
            return;
        }

        // ⭐ 使用 PlayerController 的 TeleportTo 方法来正确处理 CharacterController
        PlayerController controller = CurrentPlayerCharacter.GetComponent<PlayerController>();
        if (controller != null)
        {
            // 计算朝向向量
            Vector3 forward = m_RotationBeforeCombat * Vector3.forward;

            DebugEx.LogModule(
                "PlayerCharacterManager",
                $"准备恢复位置: Pos={m_PositionBeforeCombat}, Rot={m_RotationBeforeCombat.eulerAngles}"
            );

            // 使用 TeleportTo 方法（会正确处理 CharacterController）
            controller.TeleportTo(m_PositionBeforeCombat, forward);

            DebugEx.LogModule(
                "PlayerCharacterManager",
                $"位置恢复完成: 实际位置={CurrentPlayerCharacter.transform.position}, 实际旋转={CurrentPlayerCharacter.transform.rotation.eulerAngles}"
            );
        }
        else
        {
            // 如果没有 PlayerController，使用传统方式（不应该发生）
            DebugEx.WarningModule(
                "PlayerCharacterManager",
                "未找到 PlayerController，使用传统方式恢复位置"
            );
            CurrentPlayerCharacter.transform.position = m_PositionBeforeCombat;
            CurrentPlayerCharacter.transform.rotation = m_RotationBeforeCombat;
        }

        // 清除记录标记
        m_HasRecordedPosition = false;
    }

    #endregion
}
