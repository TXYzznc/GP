using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary>
/// 溶解过渡管理器 - 管理战斗场景切换时的溶解效果
/// </summary>
public class DissolveTransitionManager
{
    #region 单例

    private static DissolveTransitionManager s_Instance;
    public static DissolveTransitionManager Instance => s_Instance ??= new DissolveTransitionManager();

    #endregion

    #region 配置

    /// <summary>溶解动画时长（秒）</summary>
    public float DissolveDuration { get; set; } = 1.5f;

    /// <summary>环境物体的 Layer 名称</summary>
    public string EnvironmentLayerName { get; set; } = "Env_Collider";

    #endregion

    #region 私有字段

    private List<DissolveController> m_EnvironmentControllers = new List<DissolveController>();
    private List<GameObject> m_EnvironmentObjects = new List<GameObject>(); // 缓存 GameObject 引用
    private DissolveController m_ArenaController;
    private GameObject m_ArenaObject; // 缓存战斗场景 GameObject 引用
    private bool m_IsTransitioning = false;

    #endregion

    #region 公共属性

    /// <summary>是否正在过渡中</summary>
    public bool IsTransitioning => m_IsTransitioning;

    #endregion

    #region 公共方法

    /// <summary>
    /// 进入战斗场景过渡（隐藏环境，显示战斗场景）
    /// </summary>
    /// <param name="battleArena">战斗场景根物体</param>
    public async UniTask TransitionToBattle(GameObject battleArena)
    {
        // 强制重置过渡状态，防止上次异常导致卡住
        m_IsTransitioning = false;
        
        if (m_IsTransitioning) return;
        m_IsTransitioning = true;

        try
        {
            // 收集环境物体
            CollectEnvironmentObjects();

            // 设置战斗场景控制器
            SetupArenaController(battleArena);

            // 初始状态：先激活所有对象
            foreach (var obj in m_EnvironmentObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
            
            // 重新收集材质，因为刚被激活
            foreach (var ctrl in m_EnvironmentControllers)
            {
                if (ctrl != null)
                {
                    ctrl.CollectMaterials();
                    ctrl.SetDissolveAmount(0f);
                }
            }
            
            if (m_ArenaObject != null)
            {
                m_ArenaObject.SetActive(true);
            }
            if (m_ArenaController != null)
            {
                m_ArenaController.CollectMaterials();
                m_ArenaController.SetDissolveAmount(1f);
            }

            // 等待一帧确保材质激活
            await UniTask.Yield();

            // 同时播放：环境溶解隐藏 + 战斗场景溶解显示
            var tasks = new List<UniTask>();

            foreach (var ctrl in m_EnvironmentControllers)
            {
                if (ctrl != null)
                {
                    var tcs = new UniTaskCompletionSource();
                    ctrl.DissolveOut(DissolveDuration, () => tcs.TrySetResult());
                    tasks.Add(tcs.Task);
                }
            }

            if (m_ArenaController != null)
            {
                var tcs = new UniTaskCompletionSource();
                m_ArenaController.DissolveIn(DissolveDuration, () => tcs.TrySetResult());
                tasks.Add(tcs.Task);
            }

            if (tasks.Count > 0)
            {
                await UniTask.WhenAll(tasks);
            }

            // 溶解完成后隐藏环境物体
            foreach (var obj in m_EnvironmentObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }

            DebugEx.Log("[DissolveTransitionManager] 进入战斗场景过渡完成");
        }
        finally
        {
            m_IsTransitioning = false;
        }
    }

    /// <summary>
    /// 离开战斗场景过渡（显示环境，隐藏战斗场景）
    /// </summary>
    public async UniTask TransitionToExploration()
    {
        // 强制重置过渡状态
        m_IsTransitioning = false;
        
        if (m_IsTransitioning) return;
        m_IsTransitioning = true;

        try
        {
            // 先激活环境
            foreach (var obj in m_EnvironmentObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
            
            // 重新收集材质并设置初始状态
            foreach (var ctrl in m_EnvironmentControllers)
            {
                if (ctrl != null)
                {
                    ctrl.CollectMaterials();
                    ctrl.SetDissolveAmount(1f); // 从完全溶解状态开始
                }
            }

            // 确保战斗场景激活
            if (m_ArenaObject != null)
            {
                m_ArenaObject.SetActive(true);
            }
            if (m_ArenaController != null)
            {
                m_ArenaController.CollectMaterials();
                m_ArenaController.SetDissolveAmount(0f); // 从完全显示状态开始
            }

            // 等待一帧确保材质激活
            await UniTask.Yield();

            // 同时播放：环境溶解显示 + 战斗场景溶解隐藏
            var tasks = new List<UniTask>();

            foreach (var ctrl in m_EnvironmentControllers)
            {
                if (ctrl != null)
                {
                    var tcs = new UniTaskCompletionSource();
                    ctrl.DissolveIn(DissolveDuration, () => tcs.TrySetResult());
                    tasks.Add(tcs.Task);
                }
            }

            if (m_ArenaController != null)
            {
                var tcs = new UniTaskCompletionSource();
                m_ArenaController.DissolveOut(DissolveDuration, () => tcs.TrySetResult());
                tasks.Add(tcs.Task);
            }

            if (tasks.Count > 0)
            {
                await UniTask.WhenAll(tasks);
            }

            // 溶解完成后隐藏战斗场景
            if (m_ArenaObject != null)
            {
                m_ArenaObject.SetActive(false);
            }

            DebugEx.Log("[DissolveTransitionManager] 离开战斗场景过渡完成");
        }
        finally
        {
            m_IsTransitioning = false;
        }
    }

    /// <summary>
    /// 清理（切换场景时调用）
    /// </summary>
    public void Cleanup()
    {
        m_EnvironmentControllers.Clear();
        m_EnvironmentObjects.Clear();
        m_ArenaController = null;
        m_ArenaObject = null;
        m_IsTransitioning = false;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 收集所有环境物体（Env_Collider Layer 的对象）
    /// </summary>
    private void CollectEnvironmentObjects()
    {
        m_EnvironmentControllers.Clear();
        m_EnvironmentObjects.Clear();

        int envLayer = LayerMask.NameToLayer(EnvironmentLayerName);
        if (envLayer < 0)
        {
            DebugEx.Warning($"[DissolveTransitionManager] 未找到 Layer: {EnvironmentLayerName}");
            return;
        }

        // 查找所有该 Layer 的对象（包括未激活的）
        var allObjects = Object.FindObjectsOfType<GameObject>(true);
        foreach (var obj in allObjects)
        {
            if (obj.layer == envLayer)
            {
                m_EnvironmentObjects.Add(obj);
                
                // 获取或添加 DissolveController
                var controller = obj.GetComponent<DissolveController>();
                if (controller == null)
                {
                    controller = obj.AddComponent<DissolveController>();
                }
                m_EnvironmentControllers.Add(controller);
            }
        }

        DebugEx.Log($"[DissolveTransitionManager] 收集到 {m_EnvironmentControllers.Count} 个环境物体");
    }

    /// <summary>
    /// 设置战斗场景的溶解控制器
    /// </summary>
    private void SetupArenaController(GameObject battleArena)
    {
        m_ArenaObject = battleArena;
        
        if (battleArena == null)
        {
            m_ArenaController = null;
            return;
        }

        m_ArenaController = battleArena.GetComponent<DissolveController>();
        if (m_ArenaController == null)
        {
            m_ArenaController = battleArena.AddComponent<DissolveController>();
        }
    }

    #endregion
}
