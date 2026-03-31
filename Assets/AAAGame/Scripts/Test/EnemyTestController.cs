using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityGameFramework.Runtime;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 敌人测试控制器
/// 用于测试敌人生成和技能释放
/// 手动挂载在场景中的敌人上
/// </summary>
public class EnemyTestController : MonoBehaviour
{
    #region Inspector 参数

    [Header("生成配置")]
    [SerializeField]
    [Tooltip("要生成的敌人棋子ID")]
    private int m_EnemyChessId = 1;

    [SerializeField]
    [Tooltip("生成位置（单个模式）")]
    private Vector3 m_SpawnPosition = Vector3.zero;

    [Header("批量生成配置")]
    [SerializeField]
    [Tooltip("是否启用批量生成模式")]
    private bool m_EnableBatchSpawn = false;

    [SerializeField]
    [Tooltip("排列方式")]
    private SpawnArrangement m_Arrangement = SpawnArrangement.Horizontal;

    [SerializeField]
    [Tooltip("生成数量")]
    private int m_SpawnCount = 3;

    [SerializeField]
    [Tooltip("间隔距离")]
    private float m_Spacing = 2f;

    [SerializeField]
    [Tooltip("矩形排列的列数（仅矩形模式）")]
    private int m_Columns = 3;

    [SerializeField]
    [Tooltip("圆形排列的半径（仅圆形模式）")]
    private float m_CircleRadius = 5f;

    [Header("测试功能")]
    [SerializeField]
    [Tooltip("是否锁定血量（血量降到0后自动恢复）")]
    private bool m_LockHealth = false;

    [SerializeField]
    [Tooltip("每次测试伤害的数值")]
    private float m_TestDamageAmount = 50f;

    #endregion

    #region 枚举定义

    /// <summary>
    /// 排列方式
    /// </summary>
    public enum SpawnArrangement
    {
        /// <summary>横排（X轴）</summary>
        Horizontal,

        /// <summary>竖排（Z轴）</summary>
        Vertical,

        /// <summary>矩形网格</summary>
        Grid,

        /// <summary>圆形</summary>
        Circle,
    }

    #endregion

    #region 私有字段

    /// <summary>当前测试的敌人实体（单个模式）</summary>
    private ChessEntity m_CurrentEnemy;

    /// <summary>批量生成的敌人列表</summary>
    private System.Collections.Generic.List<ChessEntity> m_SpawnedEnemies =
        new System.Collections.Generic.List<ChessEntity>();

    /// <summary>是否已初始化</summary>
    private bool m_IsInitialized;

    #endregion

    #region Unity 生命周期

    private void Start()
    {
        Log.Info("[EnemyTestController] 测试控制器已启动");
    }

    private void Update()
    {
        // 快捷键已移至 Tools > Clash of Gods > Test Manager 窗口管理
        // 只在开发模式下启用
        // #if UNITY_EDITOR || DEVELOPMENT_BUILD
        // HandleTestInput();
        // #endif
    }

    private void OnDestroy()
    {
        // 清理事件监听
        if (m_CurrentEnemy != null && m_CurrentEnemy.Attribute != null)
        {
            m_CurrentEnemy.Attribute.OnHpChanged -= OnEnemyHpChanged;
        }

        // 清理批量生成的敌人
        foreach (var enemy in m_SpawnedEnemies)
        {
            if (enemy != null && enemy.Attribute != null)
            {
                enemy.Attribute.OnHpChanged -= OnEnemyHpChanged;
            }
        }
    }

    #endregion

    #region 输入按键处理

    private void HandleTestInput()
    {
        // F1 - 生成敌人
        if (Input.GetKeyDown(KeyCode.F1))
        {
            SpawnTestEnemy();
        }

        // 以下测试都需要有敌人存在
        if (m_CurrentEnemy == null)
            return;

        // F2 - 播放普通攻击动画
        if (Input.GetKeyDown(KeyCode.F2))
        {
            DoAttack();
        }

        // F3 - 释放技能1
        if (Input.GetKeyDown(KeyCode.F3))
        {
            DoSkill1();
        }

        // F4 - 释放大招
        if (Input.GetKeyDown(KeyCode.F4))
        {
            DoSkill2();
        }

        // F5 - 死亡
        if (Input.GetKeyDown(KeyCode.F5))
        {
            DoDeath();
        }

        // F6 - 刷新（销毁后重新生成）
        if (Input.GetKeyDown(KeyCode.F6))
        {
            RefreshEnemy();
        }

        // F7 - 切换锁血功能
        if (Input.GetKeyDown(KeyCode.F7))
        {
            ToggleLockHealth();
        }

        // F8 - 测试受伤
        if (Input.GetKeyDown(KeyCode.F8))
        {
            TestTakeDamage();
        }
    }

    #endregion

    #region 测试功能实现

    /// <summary>
    /// 生成测试敌人
    /// </summary>
    public async void SpawnTestEnemy()
    {
        // 如果启用批量生成模式
        if (m_EnableBatchSpawn)
        {
            await SpawnBatchEnemiesInternal();
            return;
        }

        // 单个生成模式
        // 如果有旧敌人，先销毁
        if (m_CurrentEnemy != null)
        {
            DestroyCurrentEnemy();
        }

        DebugEx.LogModule("EnemyTestController", $"生成敌人 ID={m_EnemyChessId}");

        // 使用 EnemySpawnManager 生成
        m_CurrentEnemy = await EnemySpawnManager.Instance.SpawnEnemyAsync(
            m_EnemyChessId,
            m_SpawnPosition
        );

        if (m_CurrentEnemy != null)
        {
            // 监听血量值变化事件（用于锁血功能）
            m_CurrentEnemy.Attribute.OnHpChanged += OnEnemyHpChanged;
            m_IsInitialized = true;
            DebugEx.Success("EnemyTestController", $"敌人生成成功: {m_CurrentEnemy.Config.Name}");
        }
        else
        {
            DebugEx.ErrorModule("EnemyTestController", $"敌人生成失败 ID={m_EnemyChessId}");
        }
    }

    /// <summary>
    /// 批量生成敌人
    /// </summary>
    public async void SpawnEnemiesBatch()
    {
        await SpawnBatchEnemiesInternal();
    }

    /// <summary>
    /// 批量生成敌人（内部方法）
    /// </summary>
    private async UniTask SpawnBatchEnemiesInternal()
    {
        // 清理旧的敌人
        DestroyAllEnemies();

        DebugEx.LogModule(
            "EnemyTestController",
            $"批量生成敌人 ID={m_EnemyChessId}, 数量={m_SpawnCount}, 排列={m_Arrangement}"
        );

        // 计算生成位置
        Vector3[] positions = CalculateSpawnPositions();

        // 生成敌人
        for (int i = 0; i < positions.Length; i++)
        {
            ChessEntity enemy = await EnemySpawnManager.Instance.SpawnEnemyAsync(
                m_EnemyChessId,
                positions[i]
            );

            if (enemy != null)
            {
                // 监听血量值变化事件
                enemy.Attribute.OnHpChanged += OnEnemyHpChanged;
                m_SpawnedEnemies.Add(enemy);
            }
            else
            {
                DebugEx.WarningModule("EnemyTestController", $"第 {i + 1} 个敌人生成失败");
            }
        }

        DebugEx.Success(
            "EnemyTestController",
            $"批量生成完成，成功生成 {m_SpawnedEnemies.Count} 个敌人"
        );
    }

    /// <summary>
    /// 计算生成位置
    /// </summary>
    private Vector3[] CalculateSpawnPositions()
    {
        Vector3[] positions = new Vector3[m_SpawnCount];
        Vector3 basePos = m_SpawnPosition;

        switch (m_Arrangement)
        {
            case SpawnArrangement.Horizontal:
                // 横排（X轴）
                for (int i = 0; i < m_SpawnCount; i++)
                {
                    float offset = (i - (m_SpawnCount - 1) * 0.5f) * m_Spacing;
                    positions[i] = basePos + new Vector3(offset, 0, 0);
                }
                break;

            case SpawnArrangement.Vertical:
                // 竖排（Z轴）
                for (int i = 0; i < m_SpawnCount; i++)
                {
                    float offset = (i - (m_SpawnCount - 1) * 0.5f) * m_Spacing;
                    positions[i] = basePos + new Vector3(0, 0, offset);
                }
                break;

            case SpawnArrangement.Grid:
                // 矩形网格
                int rows = Mathf.CeilToInt((float)m_SpawnCount / m_Columns);
                int index = 0;
                for (int row = 0; row < rows && index < m_SpawnCount; row++)
                {
                    for (int col = 0; col < m_Columns && index < m_SpawnCount; col++)
                    {
                        float xOffset = (col - (m_Columns - 1) * 0.5f) * m_Spacing;
                        float zOffset = (row - (rows - 1) * 0.5f) * m_Spacing;
                        positions[index] = basePos + new Vector3(xOffset, 0, zOffset);
                        index++;
                    }
                }
                break;

            case SpawnArrangement.Circle:
                // 圆形排列
                float angleStep = 360f / m_SpawnCount;
                for (int i = 0; i < m_SpawnCount; i++)
                {
                    float angle = i * angleStep * Mathf.Deg2Rad;
                    float x = Mathf.Cos(angle) * m_CircleRadius;
                    float z = Mathf.Sin(angle) * m_CircleRadius;
                    positions[i] = basePos + new Vector3(x, 0, z);
                }
                break;
        }

        return positions;
    }

    /// <summary>
    /// 执行攻击
    /// </summary>
    public void DoAttack()
    {
        if (m_CurrentEnemy == null || m_CurrentEnemy.Animator == null)
            return;

        m_CurrentEnemy.Animator.PlayAttack();
        Log.Info("[EnemyTestController] 执行普通攻击");
    }

    /// <summary>
    /// 释放技能1
    /// </summary>
    public void DoSkill1()
    {
        if (m_CurrentEnemy == null)
            return;

        if (m_CurrentEnemy.Skill1 != null && m_CurrentEnemy.Skill1.TryCast())
        {
            m_CurrentEnemy.Animator?.PlaySkill1();
            Log.Info("[EnemyTestController] 技能1释放成功");
        }
        else
        {
            // 即使没有技能逻辑也播放动画
            m_CurrentEnemy.Animator?.PlaySkill1();
            Log.Warning("[EnemyTestController] 技能1无法释放或未配置，仅播放动画");
        }
    }

    /// <summary>
    /// 释放大招
    /// </summary>
    [ContextMenu("大招")]
    public void DoSkill2()
    {
        if (m_CurrentEnemy == null)
            return;

        if (m_CurrentEnemy.Skill2 != null && m_CurrentEnemy.Skill2.TryCast())
        {
            m_CurrentEnemy.Animator?.PlaySkill2();
            Log.Info("[EnemyTestController] 大招释放成功");
        }
        else
        {
            // 即使没有技能逻辑也播放动画
            m_CurrentEnemy.Animator?.PlaySkill2();
            Log.Warning("[EnemyTestController] 大招无法释放或未配置，仅播放动画");
        }
    }

    /// <summary>
    /// 执行死亡
    /// </summary>
    [ContextMenu("死亡")]
    public void DoDeath()
    {
        if (m_CurrentEnemy == null || m_CurrentEnemy.Attribute == null)
            return;

        // 暂时关闭锁血功能
        bool wasLocked = m_LockHealth;
        m_LockHealth = false;

        // 造成足够血值为0的伤害
        m_CurrentEnemy.Attribute.TakeDamage(m_CurrentEnemy.Attribute.CurrentHp + 1, true, true);
        Log.Info("[EnemyTestController] 执行死亡");

        // 恢复锁血状态
        m_LockHealth = wasLocked;
    }

    /// <summary>
    /// 刷新敌人（销毁后重新生成）
    /// </summary>
    [ContextMenu("刷新")]
    public void RefreshEnemy()
    {
        Log.Info("[EnemyTestController] 刷新敌人");
        SpawnTestEnemy();
    }

    /// <summary>
    /// 清理所有敌人
    /// </summary>
    public void ClearAllEnemies()
    {
        DestroyAllEnemies();
        DebugEx.Success("EnemyTestController", "已清理所有敌人");
    }

    /// <summary>
    /// 切换锁血功能状态
    /// </summary>
    [ContextMenu("切换锁血功能")]
    public void ToggleLockHealth()
    {
        m_LockHealth = !m_LockHealth;
        DebugEx.LogModule("EnemyTestController", $"锁血功能: {(m_LockHealth ? "开启" : "关闭")}");
    }

    /// <summary>
    /// 测试受伤
    /// </summary>
    [ContextMenu("测试受伤")]
    public void TestTakeDamage()
    {
        if (m_CurrentEnemy == null || m_CurrentEnemy.Attribute == null)
            return;

        m_CurrentEnemy.Attribute.TakeDamage(m_TestDamageAmount, false, false);
        DebugEx.LogModule("EnemyTestController", $"测试受伤 {m_TestDamageAmount}");
    }

    /// <summary>
    /// 销毁当前敌人
    /// </summary>
    private void DestroyCurrentEnemy()
    {
        if (m_CurrentEnemy != null)
        {
            // 取消事件监听
            if (m_CurrentEnemy.Attribute != null)
            {
                m_CurrentEnemy.Attribute.OnHpChanged -= OnEnemyHpChanged;
            }

            // 销毁
            if (SummonChessManager.Instance != null)
            {
                SummonChessManager.Instance.DestroyChess(m_CurrentEnemy);
            }

            m_CurrentEnemy = null;
            m_IsInitialized = false;
        }
    }

    /// <summary>
    /// 销毁所有批量生成的敌人
    /// </summary>
    public void DestroyAllEnemies()
    {
        // 销毁单个敌人
        DestroyCurrentEnemy();

        // 销毁批量生成的敌人
        foreach (var enemy in m_SpawnedEnemies)
        {
            if (enemy != null)
            {
                // 取消事件监听
                if (enemy.Attribute != null)
                {
                    enemy.Attribute.OnHpChanged -= OnEnemyHpChanged;
                }

                // 销毁
                if (SummonChessManager.Instance != null)
                {
                    SummonChessManager.Instance.DestroyChess(enemy);
                }
            }
        }

        m_SpawnedEnemies.Clear();
        DebugEx.LogModule("EnemyTestController", "已清理所有测试敌人");
    }

    /// <summary>
    /// 敌人血量值变化回调
    /// </summary>
    private void OnEnemyHpChanged(double oldValue, double newValue)
    {
        // 如果锁血功能，血量降到0时自动恢复
        if (m_LockHealth && newValue <= 0 && m_CurrentEnemy != null)
        {
            // 延迟一帧执行，避免在事件回调中修改
            DelayedRestoreHealth();
        }
    }

    /// <summary>
    /// 延迟恢复血量值
    /// </summary>
    private async void DelayedRestoreHealth()
    {
        await UniTask.Yield();

        if (m_CurrentEnemy != null && m_CurrentEnemy.Attribute != null && m_LockHealth)
        {
            m_CurrentEnemy.Attribute.SetHp(m_CurrentEnemy.Attribute.MaxHp);
            DebugEx.LogModule("EnemyTestController", "锁血功能：血量值已恢复");
        }
    }

    #endregion

#if UNITY_EDITOR

    #region 自定义 Inspector

    [CustomEditor(typeof(EnemyTestController), true)]
    public class EnemyTestControllerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            EnemyTestController controller = (EnemyTestController)target;

            EditorGUILayout.LabelField("测试按钮", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 第一行：生成敌人 | 攻击
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("生成敌人", GUILayout.Height(30)))
            {
                controller.SpawnTestEnemy();
            }
            if (GUILayout.Button("攻击", GUILayout.Height(30)))
            {
                controller.DoAttack();
            }
            EditorGUILayout.EndHorizontal();

            // 第二行：技能1 | 大招
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("技能1", GUILayout.Height(30)))
            {
                controller.DoSkill1();
            }
            if (GUILayout.Button("大招", GUILayout.Height(30)))
            {
                controller.DoSkill2();
            }
            EditorGUILayout.EndHorizontal();

            // 第三行：死亡 | 刷新
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("死亡", GUILayout.Height(30)))
            {
                controller.DoDeath();
            }
            if (GUILayout.Button("刷新", GUILayout.Height(30)))
            {
                controller.RefreshEnemy();
            }
            EditorGUILayout.EndHorizontal();

            // 第四行：切换锁血 | 测试受伤
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("切换锁血", GUILayout.Height(30)))
            {
                controller.ToggleLockHealth();
            }
            if (GUILayout.Button("测试受伤", GUILayout.Height(30)))
            {
                controller.TestTakeDamage();
            }
            EditorGUILayout.EndHorizontal();

            // 第五行：清理所有敌人
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清理所有敌人", GUILayout.Height(30)))
            {
                controller.ClearAllEnemies();
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }

    #endregion

#endif
}
