using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameExtension;

/// <summary>
/// 战斗特效管理器
/// 负责管理战斗中的各种视觉效果：受击特效、Buff特效等
/// 伤害飘字已迁移到 DamageFloatingTextManager
/// </summary>
public static class CombatVFXManager
{
    #region 初始化

    private static bool s_IsInitialized = false;
    private static bool s_IsInitializing = false;

    /// <summary>
    /// 初始化战斗特效管理器（同步版本）
    /// </summary>
    public static void Initialize()
    {
        if (s_IsInitialized || s_IsInitializing) return;
        s_IsInitialized = true;
        Debug.Log("[CombatVFXManager] 已初始化");
    }

    /// <summary>
    /// 异步初始化战斗特效管理器
    /// </summary>
    public static async UniTaskVoid InitializeAsync()
    {
        if (s_IsInitialized || s_IsInitializing) return;
        s_IsInitializing = true;

        await UniTask.NextFrame();

        s_IsInitialized = true;
        s_IsInitializing = false;
        Debug.Log("[CombatVFXManager] 异步初始化完成");
    }

    /// <summary>
    /// 异步初始化并等待完成
    /// </summary>
    public static async UniTask InitializeAndWaitAsync()
    {
        if (s_IsInitialized) return;
        if (s_IsInitializing)
        {
            await UniTask.WaitUntil(() => s_IsInitialized || !s_IsInitializing);
            return;
        }

        s_IsInitializing = true;
        await UniTask.NextFrame();
        s_IsInitialized = true;
        s_IsInitializing = false;
        Debug.Log("[CombatVFXManager] 初始化完成");
    }

    #endregion

    #region 特效对象池

    /// <summary>特效对象池</summary>
    private static Dictionary<int, Queue<GameObject>> s_EffectPools = new Dictionary<int, Queue<GameObject>>();

    /// <summary>活跃的特效实例</summary>
    private static List<EffectInstance> s_ActiveEffects = new List<EffectInstance>();

    /// <summary>Buff特效实例（按目标和BuffId索引）</summary>
    private static Dictionary<Transform, Dictionary<int, GameObject>> s_BuffEffects = new Dictionary<Transform, Dictionary<int, GameObject>>();

    /// <summary>
    /// 特效实例数据
    /// </summary>
    private class EffectInstance
    {
        public GameObject GameObject;
        public int EffectId;
        public float RemainingTime;
        public ParticleSystem ParticleSystem;
    }

    #endregion

    #region 特效播放

    /// <summary>
    /// 播放特效（通用方法）
    /// </summary>
    /// <param name="effectId">特效资源ID（ResourceConfigTable）</param>
    /// <param name="position">播放位置</param>
    /// <param name="rotation">播放旋转（可选，默认无旋转）</param>
    /// <param name="duration">持续时间（0=自动检测粒子系统时长，-1=永久）</param>
    /// <returns>特效GameObject实例</returns>
    public static GameObject PlayEffect(int effectId, Vector3 position, Quaternion? rotation = null, float duration = 0f)
    {
        if (effectId <= 0) return null;

        // 异步加载并播放
        PlayEffectAsync(effectId, position, rotation ?? Quaternion.identity, duration).Forget();
        return null; // 异步方法无法立即返回实例
    }

    /// <summary>
    /// 异步播放特效
    /// </summary>
    private static async UniTaskVoid PlayEffectAsync(int effectId, Vector3 position, Quaternion rotation, float duration)
    {
        GameObject effectObj = await GetOrCreateEffectAsync(effectId);
        if (effectObj == null) return;

        // 设置位置和旋转
        effectObj.transform.position = position;
        effectObj.transform.rotation = rotation;
        effectObj.SetActive(true);

        // 获取粒子系统
        var ps = effectObj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
        }

        // 计算持续时间
        float actualDuration = duration;
        if (actualDuration == 0f && ps != null)
        {
            // 自动检测粒子系统时长
            actualDuration = ps.main.duration + ps.main.startLifetime.constantMax;
        }
        if (actualDuration <= 0f)
        {
            actualDuration = 2f; // 默认2秒
        }

        // 添加到活跃列表
        var instance = new EffectInstance
        {
            GameObject = effectObj,
            EffectId = effectId,
            RemainingTime = actualDuration,
            ParticleSystem = ps
        };
        s_ActiveEffects.Add(instance);

        Debug.Log($"[CombatVFXManager] PlayEffect: effectId={effectId}, pos={position}, duration={actualDuration:F2}s");
    }

    /// <summary>
    /// 在目标位置播放特效（跟随目标）
    /// </summary>
    public static async UniTask<GameObject> PlayEffectAtTargetAsync(int effectId, Transform target, Vector3 offset = default, float duration = 0f)
    {
        if (effectId <= 0 || target == null) return null;

        GameObject effectObj = await GetOrCreateEffectAsync(effectId);
        if (effectObj == null) return null;

        // 设置为目标的子物体
        effectObj.transform.SetParent(target);
        effectObj.transform.localPosition = offset;
        effectObj.transform.localRotation = Quaternion.identity;
        effectObj.SetActive(true);

        var ps = effectObj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
        }

        float actualDuration = duration;
        if (actualDuration == 0f && ps != null)
        {
            actualDuration = ps.main.duration + ps.main.startLifetime.constantMax;
        }
        if (actualDuration <= 0f)
        {
            actualDuration = 2f;
        }

        var instance = new EffectInstance
        {
            GameObject = effectObj,
            EffectId = effectId,
            RemainingTime = actualDuration,
            ParticleSystem = ps
        };
        s_ActiveEffects.Add(instance);

        return effectObj;
    }

    /// <summary>
    /// 播放受击特效
    /// </summary>
    public static void PlayHitEffect(Transform target, int hitEffectId)
    {
        if (target == null || hitEffectId <= 0) return;

        Vector3 position = target.position + Vector3.up * 1f; // 稍微抬高
        PlayEffect(hitEffectId, position);
    }

    /// <summary>
    /// 播放技能释放特效（在施法者位置）
    /// </summary>
    public static void PlaySkillEffect(Transform caster, int effectId, Vector3 offset = default)
    {
        if (caster == null || effectId <= 0) return;

        Vector3 position = caster.position + caster.TransformDirection(offset);
        PlayEffect(effectId, position, caster.rotation);
    }

    /// <summary>
    /// 获取或创建特效实例
    /// </summary>
    private static async UniTask<GameObject> GetOrCreateEffectAsync(int effectId)
    {
        // 检查对象池
        if (s_EffectPools.TryGetValue(effectId, out var pool) && pool.Count > 0)
        {
            return pool.Dequeue();
        }

        // 加载预制体
        var prefab = await GameExtension.ResourceExtension.LoadPrefabAsync(effectId);
        if (prefab == null)
        {
            Debug.LogWarning($"[CombatVFXManager] 加载特效预制体失败: effectId={effectId}");
            return null;
        }

        // 实例化
        var instance = Object.Instantiate(prefab);
        instance.name = $"Effect_{effectId}";

        return instance;
    }

    /// <summary>
    /// 回收特效到对象池
    /// </summary>
    private static void ReturnEffectToPool(int effectId, GameObject effectObj)
    {
        if (effectObj == null) return;

        effectObj.SetActive(false);
        effectObj.transform.SetParent(null);

        if (!s_EffectPools.TryGetValue(effectId, out var pool))
        {
            pool = new Queue<GameObject>();
            s_EffectPools[effectId] = pool;
        }

        // 限制池大小
        if (pool.Count < 5)
        {
            pool.Enqueue(effectObj);
        }
        else
        {
            Object.Destroy(effectObj);
        }
    }

    #endregion

    #region Buff特效

    /// <summary>
    /// 播放Buff附加特效（持续显示直到移除）
    /// </summary>
    public static async void PlayBuffEffect(Transform target, int buffEffectId)
    {
        if (target == null || buffEffectId <= 0) return;

        // 检查是否已有该Buff特效
        if (s_BuffEffects.TryGetValue(target, out var buffDict))
        {
            if (buffDict.ContainsKey(buffEffectId))
            {
                Debug.Log($"[CombatVFXManager] Buff特效已存在: target={target.name}, effectId={buffEffectId}");
                return;
            }
        }
        else
        {
            buffDict = new Dictionary<int, GameObject>();
            s_BuffEffects[target] = buffDict;
        }

        // 创建特效
        var effectObj = await GetOrCreateEffectAsync(buffEffectId);
        if (effectObj == null) return;

        // 设置为目标子物体
        effectObj.transform.SetParent(target);
        effectObj.transform.localPosition = Vector3.up * 0.5f; // 稍微抬高
        effectObj.transform.localRotation = Quaternion.identity;
        effectObj.SetActive(true);

        var ps = effectObj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.loop = true; // 确保循环播放
            ps.Play();
        }

        // 记录
        buffDict[buffEffectId] = effectObj;

        Debug.Log($"[CombatVFXManager] PlayBuffEffect: target={target.name}, effectId={buffEffectId}");
    }

    /// <summary>
    /// 停止Buff特效
    /// </summary>
    public static void StopBuffEffect(Transform target, int buffEffectId)
    {
        if (target == null || buffEffectId <= 0) return;

        if (!s_BuffEffects.TryGetValue(target, out var buffDict)) return;
        if (!buffDict.TryGetValue(buffEffectId, out var effectObj)) return;

        // 移除记录
        buffDict.Remove(buffEffectId);
        if (buffDict.Count == 0)
        {
            s_BuffEffects.Remove(target);
        }

        // 停止并回收
        if (effectObj != null)
        {
            var ps = effectObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop();
            }
            ReturnEffectToPool(buffEffectId, effectObj);
        }

        Debug.Log($"[CombatVFXManager] StopBuffEffect: target={target.name}, effectId={buffEffectId}");
    }

    /// <summary>
    /// 停止目标上的所有Buff特效
    /// </summary>
    public static void StopAllBuffEffects(Transform target)
    {
        if (target == null) return;

        if (!s_BuffEffects.TryGetValue(target, out var buffDict)) return;

        foreach (var kvp in buffDict)
        {
            if (kvp.Value != null)
            {
                var ps = kvp.Value.GetComponent<ParticleSystem>();
                if (ps != null) ps.Stop();
                ReturnEffectToPool(kvp.Key, kvp.Value);
            }
        }

        s_BuffEffects.Remove(target);
    }

    #endregion

    #region 特效更新

    /// <summary>
    /// 每帧更新（由 CombatVFXUpdater.LateUpdate 调用）
    /// </summary>
    public static void LateUpdate()
    {
        // 特效系统暂无需要在 LateUpdate 中处理的逻辑
        // 特效的时间更新通过 UpdateEffects(deltaTime) 进行
    }

    /// <summary>
    /// 更新特效（需要在某处调用，如CombatState的Update）
    /// </summary>
    public static void UpdateEffects(float deltaTime)
    {
        // 更新活跃特效
        for (int i = s_ActiveEffects.Count - 1; i >= 0; i--)
        {
            var instance = s_ActiveEffects[i];
            if (instance.GameObject == null)
            {
                s_ActiveEffects.RemoveAt(i);
                continue;
            }

            instance.RemainingTime -= deltaTime;
            if (instance.RemainingTime <= 0)
            {
                // 回收
                ReturnEffectToPool(instance.EffectId, instance.GameObject);
                s_ActiveEffects.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 清理（战斗结束或场景切换时调用）
    /// </summary>
    public static void Cleanup()
    {
        // 回收所有活跃的特效
        foreach (var effect in s_ActiveEffects)
        {
            if (effect?.GameObject != null)
            {
                ReturnEffectToPool(effect.EffectId, effect.GameObject);
            }
        }
        s_ActiveEffects.Clear();

        // 销毁所有对象池中的对象
        foreach (var pool in s_EffectPools.Values)
        {
            while (pool.Count > 0)
            {
                var obj = pool.Dequeue();
                if (obj != null)
                {
                    Object.Destroy(obj);
                }
            }
        }
        s_EffectPools.Clear();

        // 清理Buff特效
        foreach (var targetEffects in s_BuffEffects.Values)
        {
            foreach (var effect in targetEffects.Values)
            {
                if (effect != null)
                {
                    Object.Destroy(effect);
                }
            }
        }
        s_BuffEffects.Clear();

        s_IsInitialized = false;
        Debug.Log("[CombatVFXManager] 已清理");
    }

    #endregion
}
