using Cysharp.Threading.Tasks;
using GameFramework.DataTable;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 黑暗哮天犬管理器
/// 管理黑暗哮天犬的生成、属性继承、销毁
/// </summary>
public class DarkWolfdogManager
{
    #region 字段

    private ChessEntity m_Master; // 主人（黑暗杨戬）
    private ChessEntity m_Wolfdog; // 黑暗哮天犬
    private ChessContext m_WolfdogContext; // 哮天犬的上下文

    #endregion

    #region 公共方法

    public void Init(ChessEntity master)
    {
        m_Master = master;
        m_Wolfdog = null;
    }

    /// <summary>
    /// 生成黑暗哮天犬
    /// </summary>
    /// <param name="spawnData">生成配置</param>
    public async UniTask SpawnWolfdogAsync(DarkYangyuanPhaseConfig.SubordinateSpawnData spawnData)
    {
        if (m_Master == null)
            return;

        // 如果已存在就先移除
        if (m_Wolfdog != null)
        {
            RemoveWolfdog();
        }

        if (!ChessDataManager.Instance.TryGetConfig(spawnData.ChessId, out var wolfdogConfig))
        {
            DebugEx.Error("DarkWolfdogManager", $"棋子配置 {spawnData.ChessId} 不存在");
            return;
        }

        // 计算生成位置（主人右侧）
        Vector3 spawnPosition = m_Master.transform.position + m_Master.transform.right * 2f;

        // 使用 SummonChessManager 生成棋子
        var manager = SummonChessManager.Instance;
        if (manager == null)
        {
            DebugEx.Error("DarkWolfdogManager", "无法获取 SummonChessManager");
            return;
        }

        m_Wolfdog = await manager.SpawnChessAsync(spawnData.ChessId, spawnPosition, m_Master.Camp);
        if (m_Wolfdog == null)
        {
            DebugEx.Error("DarkWolfdogManager", $"生成黑暗哮天犬失败 (ChessId={spawnData.ChessId})");
            return;
        }

        // 初始化属性继承
        InitializeWolfdogAttributes(wolfdogConfig, spawnData.InheritRatio);

        DebugEx.Success("DarkWolfdogManager",
            $"黑暗哮天犬生成成功 (ChessId={spawnData.ChessId}, 继承比例={spawnData.InheritRatio:P0})");
    }

    /// <summary>
    /// 销毁黑暗哮天犬
    /// </summary>
    public void RemoveWolfdog()
    {
        if (m_Wolfdog == null)
            return;

        DebugEx.Log("DarkWolfdogManager", "移除黑暗哮天犬");

        // 直接销毁游戏对象（哮天犬是属下单位，主人死亡或阶段切换时销毁）
        if (m_Wolfdog.gameObject != null)
        {
            UnityEngine.Object.Destroy(m_Wolfdog.gameObject);
        }

        m_Wolfdog = null;
        m_WolfdogContext = null;
    }

    /// <summary>
    /// 获取哮天犬实体
    /// </summary>
    public ChessEntity GetWolfdog() => m_Wolfdog;

    /// <summary>
    /// 哮天犬是否存活
    /// </summary>
    public bool IsWolfdogAlive => m_Wolfdog != null && !m_Wolfdog.Attribute.IsDead;

    #endregion

    #region 私有方法

    /// <summary>
    /// 初始化哮天犬属性（属性继承和自定义）
    /// </summary>
    private void InitializeWolfdogAttributes(SummonChessConfig wolfdogConfig, double inheritRatio)
    {
        if (m_Wolfdog?.Attribute == null || m_Master?.Attribute == null)
            return;

        // 应用属性继承
        m_Wolfdog.Attribute.InitializeAsSubordinate(m_Wolfdog, wolfdogConfig, m_Master.Attribute, inheritRatio);
    }

    #endregion
}
