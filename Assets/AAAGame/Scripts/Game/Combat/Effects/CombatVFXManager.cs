using UnityEngine;
using System.Collections.Generic;
using TMPro;
using Cysharp.Threading.Tasks;
using GameExtension;

/// <summary>
/// 战斗特效管理器
/// 负责管理战斗中的各种视觉效果：伤害飘字、受击特效、Buff特效等
/// </summary>
public static class CombatVFXManager
{
    #region 常量

    /// <summary>飘字预制体资源ID（ResourceConfigTable）</summary>
    private const int DAMAGE_POPUP_PREFAB_ID = 2401;

    /// <summary>WorldCanvas 对象名称</summary>
    private const string WORLD_CANVAS_NAME = "WorldCanvas";

    #endregion

    #region 伤害类型枚举

    /// <summary>
    /// 伤害类型
    /// </summary>
    public enum DamageType
    {
        /// <summary>物理伤害</summary>
        Physical,
        /// <summary>魔法伤害</summary>
        Magic,
        /// <summary>真实伤害</summary>
        True,
        /// <summary>暴击伤害</summary>
        Critical,
        /// <summary>治疗</summary>
        Heal,
        /// <summary>护盾</summary>
        Shield,
        /// <summary>格挡/免疫</summary>
        Block,

        // Buff 持续伤害类型
        /// <summary>Buff 伤害（通用）</summary>
        BuffDamage,
        /// <summary>灼烧伤害</summary>
        BurnDamage,
        /// <summary>中毒伤害</summary>
        PoisonDamage,
        /// <summary>流血伤害</summary>
        BleedDamage,
        /// <summary>冰霜伤害</summary>
        FrostDamage
    }

    #endregion

    #region 配置

    /// <summary>伤害飘字配置</summary>
    public static class DamagePopupConfig
    {
        public static float BaseHeightOffset = 1.2f;
        /// <summary>飘字持续时间</summary>
        public static float Duration = 1.0f;
        /// <summary>飘字上升高度</summary>
        public static float RiseHeight = 1.0f;
        /// <summary>飘字水平随机偏移范围</summary>
        public static float HorizontalRandomRange = 0.25f;
        /// <summary>飘字初始缩放</summary>
        public static float InitialScale = 1.0f;
        /// <summary>飘字最大缩放（暴击时）</summary>
        public static float CriticalScale = 1.5f;
        /// <summary>飘字字体大小</summary>
        public static float FontSize = 28f;
        /// <summary>暴击字体大小</summary>
        public static float CriticalFontSize = 36f;

        // 直接伤害颜色配置
        public static Color PhysicalColor = new Color(1f, 0.9f, 0.6f);      // 淡黄色
        public static Color MagicColor = new Color(0.6f, 0.6f, 1f);         // 淡蓝色
        public static Color TrueColor = new Color(1f, 1f, 1f);              // 白色
        public static Color CriticalColor = new Color(1f, 0.3f, 0.3f);      // 红色
        public static Color HealColor = new Color(0.3f, 1f, 0.3f);          // 绿色
        public static Color ShieldColor = new Color(0.8f, 0.8f, 0.8f);      // 灰色
        public static Color BlockColor = new Color(0.5f, 0.5f, 0.5f);       // 深灰色

        // Buff 伤害专属配置
        /// <summary>Buff 伤害飘字持续时间（更短）</summary>
        public static float BuffDuration = 0.8f;
        /// <summary>Buff 伤害飘字上升高度（更低）</summary>
        public static float BuffRiseHeight = 0.6f;
        /// <summary>Buff 伤害飘字初始缩放（更小）</summary>
        public static float BuffInitialScale = 0.8f;
        /// <summary>Buff 伤害飘字初始透明度（略透明）</summary>
        public static float BuffInitialAlpha = 0.9f;
        /// <summary>Buff 伤害飘字水平偏移（避免重叠）</summary>
        public static float BuffHorizontalOffset = 0.18f;
        /// <summary>Buff 伤害飘字字体大小（略小）</summary>
        public static float BuffFontSize = 22f;

        // Buff 伤害颜色配置（用于渐变）
        public static Color BurnColor = new Color(1f, 0.4f, 0.1f);          // 橙红色（火焰感）
        public static Color PoisonColor = new Color(0.5f, 0.8f, 0.3f);      // 紫绿色（毒素感）
        public static Color BleedColor = new Color(0.8f, 0.1f, 0.1f);       // 深红色（血液感）
        public static Color FrostColor = new Color(0.3f, 0.7f, 1f);         // 冰蓝色（冰霜感）
        public static Color BuffDamageColor = new Color(0.9f, 0.6f, 0.3f);  // 通用 Buff 伤害颜色

        // Buff 伤害渐变配置（四角渐变：左下、左上、右上、右下）
        /// <summary>灼烧伤害渐变（橙红 → 黄色）</summary>
        public static Color[] BurnGradient = new Color[]
        {
            new Color(1f, 0.3f, 0f),    // 左下：深橙红
            new Color(1f, 0.5f, 0.1f),  // 左上：橙红
            new Color(1f, 0.8f, 0.2f),  // 右上：橙黄
            new Color(1f, 0.6f, 0.1f)   // 右下：橙色
        };

        /// <summary>中毒伤害渐变（紫绿 → 绿色）</summary>
        public static Color[] PoisonGradient = new Color[]
        {
            new Color(0.3f, 0.6f, 0.2f),  // 左下：深绿
            new Color(0.5f, 0.8f, 0.3f),  // 左上：绿色
            new Color(0.6f, 1f, 0.4f),    // 右上：亮绿
            new Color(0.4f, 0.7f, 0.3f)   // 右下：中绿
        };

        /// <summary>流血伤害渐变（深红 → 红色）</summary>
        public static Color[] BleedGradient = new Color[]
        {
            new Color(0.6f, 0f, 0f),      // 左下：暗红
            new Color(0.8f, 0.1f, 0.1f),  // 左上：深红
            new Color(1f, 0.2f, 0.2f),    // 右上：红色
            new Color(0.7f, 0.05f, 0.05f) // 右下：血红
        };

        /// <summary>冰霜伤害渐变（深蓝 → 冰蓝）</summary>
        public static Color[] FrostGradient = new Color[]
        {
            new Color(0.2f, 0.5f, 0.8f),  // 左下：深蓝
            new Color(0.3f, 0.7f, 1f),    // 左上：冰蓝
            new Color(0.5f, 0.9f, 1f),    // 右上：亮蓝
            new Color(0.3f, 0.6f, 0.9f)   // 右下：中蓝
        };
    }

    #endregion

    #region 私有字段

    private static Transform s_PopupContainer;
    private static Queue<DamagePopupItem> s_PopupPool = new Queue<DamagePopupItem>();
    private static List<DamagePopupItem> s_ActivePopups = new List<DamagePopupItem>();
    private static GameObject s_PopupPrefab;
    private static Camera s_MainCamera;
    private static bool s_IsInitialized = false;
    private static bool s_IsInitializing = false;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化战斗特效管理器（同步版本，需要预制体已加载）
    /// 在战斗开始时调用
    /// </summary>
    public static void Initialize()
    {
        if (s_IsInitialized || s_IsInitializing) return;

        // 启动异步初始化
        InitializeAsync().Forget();
    }

    /// <summary>
    /// 异步初始化战斗特效管理器
    /// </summary>
    public static async UniTaskVoid InitializeAsync()
    {
        if (s_IsInitialized || s_IsInitializing) return;
        s_IsInitializing = true;

        try
        {
            // 1. 查找场景中的 WorldCanvas
            var worldCanvasGo = GameObject.Find(WORLD_CANVAS_NAME);
            if (worldCanvasGo == null)
            {
                DebugEx.ErrorModule("CombatVFXManager", $"未找到 {WORLD_CANVAS_NAME} 对象！请在场景中创建该对象。");
                s_IsInitializing = false;
                return;
            }
            LayerHelper.SetLayerRecursively(worldCanvasGo, LayerHelper.Layer.Default);
            s_PopupContainer = worldCanvasGo.transform;

            // 2. 获取主摄像机
            s_MainCamera = CameraRegistry.PlayerCamera;

            // 3. 从 ResourceConfigTable 加载飘字预制体
            s_PopupPrefab = await ResourceExtension.LoadPrefabAsync(DAMAGE_POPUP_PREFAB_ID);
            if (s_PopupPrefab == null)
            {
                DebugEx.ErrorModule("CombatVFXManager", $"加载飘字预制体失败！ResourceId={DAMAGE_POPUP_PREFAB_ID}");
                s_IsInitializing = false;
                return;
            }

            // 4. 预热对象池
            PrewarmPool(10);

            s_IsInitialized = true;
            s_IsInitializing = false;
            Debug.Log("[CombatVFXManager] 初始化完成");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CombatVFXManager] 初始化异常: {ex.Message}");
            s_IsInitializing = false;
        }
    }

    /// <summary>
    /// 异步初始化并等待完成
    /// </summary>
    public static async UniTask InitializeAndWaitAsync()
    {
        if (s_IsInitialized) return;
        if (s_IsInitializing)
        {
            // 等待初始化完成
            await UniTask.WaitUntil(() => s_IsInitialized || !s_IsInitializing);
            return;
        }

        s_IsInitializing = true;

        try
        {
            var worldCanvasGo = GameObject.Find(WORLD_CANVAS_NAME);
            if (worldCanvasGo == null)
            {
                Debug.LogError($"[CombatVFXManager] 未找到 {WORLD_CANVAS_NAME} 对象！");
                s_IsInitializing = false;
                return;
            }
            LayerHelper.SetLayerRecursively(worldCanvasGo, LayerHelper.Layer.Default);
            s_PopupContainer = worldCanvasGo.transform;

            s_MainCamera = CameraRegistry.PlayerCamera;

            s_PopupPrefab = await ResourceExtension.LoadPrefabAsync(DAMAGE_POPUP_PREFAB_ID);
            if (s_PopupPrefab == null)
            {
                Debug.LogError($"[CombatVFXManager] 加载飘字预制体失败！");
                s_IsInitializing = false;
                return;
            }

            PrewarmPool(10);

            s_IsInitialized = true;
            s_IsInitializing = false;
            Debug.Log("[CombatVFXManager] 初始化完成");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CombatVFXManager] 初始化异常: {ex.Message}");
            s_IsInitializing = false;
        }
    }

    /// <summary>
    /// 清理（战斗结束或场景切换时调用）
    /// </summary>
    public static void Cleanup()
    {
        // 回收所有活跃的飘字
        foreach (var popup in s_ActivePopups)
        {
            if (popup != null)
            {
                popup.gameObject.SetActive(false);
                s_PopupPool.Enqueue(popup);
            }
        }
        s_ActivePopups.Clear();

        // 销毁对象池中的所有对象
        while (s_PopupPool.Count > 0)
        {
            var popup = s_PopupPool.Dequeue();
            if (popup != null)
            {
                Object.Destroy(popup.gameObject);
            }
        }

        s_PopupContainer = null;
        s_PopupPrefab = null;
        s_IsInitialized = false;
        s_IsInitializing = false;
        Debug.Log("[CombatVFXManager] 已清理");
    }

    /// <summary>
    /// 销毁（完全释放资源）
    /// </summary>
    public static void Destroy()
    {
        Cleanup();
    }

    #endregion

    #region 伤害飘字

    /// <summary>
    /// 显示伤害飘字
    /// </summary>
    /// <param name="worldPosition">世界坐标位置</param>
    /// <param name="damage">伤害值</param>
    /// <param name="damageType">伤害类型</param>
    public static void ShowDamagePopup(Vector3 worldPosition, double damage, DamageType damageType = DamageType.Physical)
    {
        if (!s_IsInitialized)
        {
            Debug.LogWarning("[CombatVFXManager] 尚未初始化完成，无法显示飘字");
            return;
        }

        // 从对象池获取飘字
        var popup = GetPopupFromPool();
        if (popup == null) return;

        // 配置飘字
        ConfigurePopup(popup, worldPosition, damage, damageType);

        // 添加到活跃列表
        s_ActivePopups.Add(popup);

        // 播放动画
        popup.Play(() =>
        {
            // 动画完成后回收
            s_ActivePopups.Remove(popup);
            ReturnPopupToPool(popup);
        });

        Debug.Log($"[CombatVFXManager] ShowDamagePopup: pos={worldPosition}, damage={damage}, type={damageType}");
    }

    /// <summary>
    /// 显示伤害飘字（简化版本）
    /// </summary>
    /// <param name="target">目标 Transform</param>
    /// <param name="damage">伤害值</param>
    /// <param name="isMagic">是否为魔法伤害</param>
    /// <param name="isCritical">是否暴击</param>
    public static void ShowDamage(Transform target, double damage, bool isMagic = false, bool isCritical = false)
    {
        if (target == null) return;

        // 计算显示位置（目标头顶上方）
        Vector3 position = target.position + Vector3.up * DamagePopupConfig.BaseHeightOffset;

        // 确定伤害类型
        DamageType type;
        if (isCritical)
        {
            type = DamageType.Critical;
        }
        else if (isMagic)
        {
            type = DamageType.Magic;
        }
        else
        {
            type = DamageType.Physical;
        }

        ShowDamagePopup(position, damage, type);
    }

    /// <summary>
    /// 显示治疗飘字
    /// </summary>
    public static void ShowHeal(Transform target, double amount)
    {
        if (target == null) return;
        Vector3 position = target.position + Vector3.up * DamagePopupConfig.BaseHeightOffset;
        ShowDamagePopup(position, amount, DamageType.Heal);
    }

    /// <summary>
    /// 显示护盾飘字
    /// </summary>
    public static void ShowShield(Transform target, double amount)
    {
        if (target == null) return;
        Vector3 position = target.position + Vector3.up * DamagePopupConfig.BaseHeightOffset;
        ShowDamagePopup(position, amount, DamageType.Shield);
    }

    /// <summary>
    /// 显示格挡/免疫文字
    /// </summary>
    public static void ShowBlock(Transform target, string text = "格挡")
    {
        if (target == null) return;
        if (!s_IsInitialized)
        {
            Debug.LogWarning("[CombatVFXManager] 尚未初始化完成，无法显示飘字");
            return;
        }

        Vector3 position = target.position + Vector3.up * DamagePopupConfig.BaseHeightOffset;

        var popup = GetPopupFromPool();
        if (popup == null) return;

        popup.SetText(text);
        popup.SetColor(DamagePopupConfig.BlockColor);
        popup.SetPosition(position);
        popup.SetScale(DamagePopupConfig.InitialScale);
        popup.SetFontSize(DamagePopupConfig.FontSize);
        popup.SetTextStyle(useBold: true, useGradient: false);
        popup.SetInitialAlpha(1f);
        popup.SetAnimationParams(DamagePopupConfig.Duration, DamagePopupConfig.RiseHeight);

        s_ActivePopups.Add(popup);
        popup.Play(() =>
        {
            s_ActivePopups.Remove(popup);
            ReturnPopupToPool(popup);
        });
    }

    #endregion

    #region 特效播放

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

    #endregion

    #region 私有方法 - 对象池

    /// <summary>
    /// 预热对象池
    /// </summary>
    private static void PrewarmPool(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var popup = CreatePopupInstance();
            if (popup != null)
            {
                popup.gameObject.SetActive(false);
                s_PopupPool.Enqueue(popup);
            }
        }
    }

    /// <summary>
    /// 创建飘字实例
    /// </summary>
    private static DamagePopupItem CreatePopupInstance()
    {
        if (s_PopupPrefab == null || s_PopupContainer == null)
        {
            Debug.LogError("[CombatVFXManager] 预制体或容器为空，无法创建飘字实例");
            return null;
        }

        var go = Object.Instantiate(s_PopupPrefab, s_PopupContainer);
        LayerHelper.SetLayerRecursively(go, LayerHelper.Layer.Default);
        var popupItem = go.GetComponent<DamagePopupItem>();

        // 如果预制体上没有 DamagePopupItem 组件，添加一个
        if (popupItem == null)
        {
            popupItem = go.AddComponent<DamagePopupItem>();
        }

        // 无论组件是否已存在，都需要初始化（确保 m_Text 引用正确）
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            popupItem.Initialize(tmp);
        }
        else
        {
            Debug.LogWarning("[CombatVFXManager] 飘字预制体中未找到 TextMeshProUGUI 组件");
        }

        return popupItem;
    }

    /// <summary>
    /// 从对象池获取飘字
    /// </summary>
    private static DamagePopupItem GetPopupFromPool()
    {
        DamagePopupItem popup;

        if (s_PopupPool.Count > 0)
        {
            popup = s_PopupPool.Dequeue();
        }
        else
        {
            popup = CreatePopupInstance();
        }

        if (popup != null)
        {
            popup.gameObject.SetActive(true);
        }

        return popup;
    }

    /// <summary>
    /// 归还飘字到对象池
    /// </summary>
    private static void ReturnPopupToPool(DamagePopupItem popup)
    {
        if (popup == null) return;

        popup.gameObject.SetActive(false);
        s_PopupPool.Enqueue(popup);
    }

    /// <summary>
    /// 配置飘字
    /// </summary>
    private static void ConfigurePopup(DamagePopupItem popup, Vector3 worldPosition, double damage, DamageType damageType)
    {
        // 判断是否为 Buff 伤害
        bool isBuffDamage = damageType >= DamageType.BuffDamage;

        // 设置文本
        string text;
        if (damageType == DamageType.Heal || damageType == DamageType.Shield)
        {
            text = $"+{damage:F0}";
        }
        else
        {
            text = $"-{damage:F0}";
        }
        popup.SetText(text);

        // 设置颜色
        Color color = GetDamageColor(damageType);
        popup.SetColor(color);

        // 设置位置（添加随机偏移）
        float horizontalRange = isBuffDamage
            ? DamagePopupConfig.BuffHorizontalOffset
            : DamagePopupConfig.HorizontalRandomRange;
        float randomX = Random.Range(-horizontalRange, horizontalRange);

        // Buff 伤害额外偏移，避免与直接伤害重叠
        if (isBuffDamage)
        {
            randomX += DamagePopupConfig.BuffHorizontalOffset;
        }

        Vector3 position = worldPosition + new Vector3(randomX, 0, 0);
        popup.SetPosition(position);

        // 设置缩放
        float scale;
        if (damageType == DamageType.Critical)
        {
            scale = DamagePopupConfig.CriticalScale;
        }
        else if (isBuffDamage)
        {
            scale = DamagePopupConfig.BuffInitialScale;
        }
        else
        {
            scale = DamagePopupConfig.InitialScale;
        }
        popup.SetScale(scale);

        // 设置字体大小
        float fontSize;
        if (damageType == DamageType.Critical)
        {
            fontSize = DamagePopupConfig.CriticalFontSize;
        }
        else if (isBuffDamage)
        {
            fontSize = DamagePopupConfig.BuffFontSize;
        }
        else
        {
            fontSize = DamagePopupConfig.FontSize;
        }
        popup.SetFontSize(fontSize);

        // 设置文本样式（直接伤害加粗，Buff 伤害使用渐变）
        if (isBuffDamage)
        {
            // Buff 伤害：正常文本 + 颜色渐变
            Color[] gradientColors = GetBuffGradientColors(damageType);
            popup.SetTextStyle(useBold: false, useGradient: true, gradientColors: gradientColors);

            // 设置初始透明度（略透明）
            popup.SetInitialAlpha(DamagePopupConfig.BuffInitialAlpha);
            // Buff 伤害：更短的持续时间和更低的上升高度
            popup.SetAnimationParams(DamagePopupConfig.BuffDuration, DamagePopupConfig.BuffRiseHeight);
        }
        else
        {
            // 直接伤害：加粗文本 + 无渐变
            popup.SetTextStyle(useBold: true, useGradient: false);

            // 完全不透明
            popup.SetInitialAlpha(1f);
            // 直接伤害：使用默认动画参数
            popup.SetAnimationParams(DamagePopupConfig.Duration, DamagePopupConfig.RiseHeight);
        }

        
    }

    /// <summary>
    /// 获取伤害类型对应的颜色
    /// </summary>
    private static Color GetDamageColor(DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Physical => DamagePopupConfig.PhysicalColor,
            DamageType.Magic => DamagePopupConfig.MagicColor,
            DamageType.True => DamagePopupConfig.TrueColor,
            DamageType.Critical => DamagePopupConfig.CriticalColor,
            DamageType.Heal => DamagePopupConfig.HealColor,
            DamageType.Shield => DamagePopupConfig.ShieldColor,
            DamageType.Block => DamagePopupConfig.BlockColor,
            DamageType.BurnDamage => DamagePopupConfig.BurnColor,
            DamageType.PoisonDamage => DamagePopupConfig.PoisonColor,
            DamageType.BleedDamage => DamagePopupConfig.BleedColor,
            DamageType.FrostDamage => DamagePopupConfig.FrostColor,
            DamageType.BuffDamage => DamagePopupConfig.BuffDamageColor,
            _ => Color.white
        };
    }

    /// <summary>
    /// 获取 Buff 伤害的渐变颜色
    /// </summary>
    private static Color[] GetBuffGradientColors(DamageType damageType)
    {
        return damageType switch
        {
            DamageType.BurnDamage => DamagePopupConfig.BurnGradient,
            DamageType.PoisonDamage => DamagePopupConfig.PoisonGradient,
            DamageType.BleedDamage => DamagePopupConfig.BleedGradient,
            DamageType.FrostDamage => DamagePopupConfig.FrostGradient,
            _ => new Color[]  // 默认渐变（橙黄色）
            {
                new Color(0.9f, 0.5f, 0.2f),
                new Color(1f, 0.6f, 0.3f),
                new Color(1f, 0.7f, 0.4f),
                new Color(0.95f, 0.55f, 0.25f)
            }
        };
    }

    #endregion

    #region 每帧更新

    /// <summary>
    /// 更新所有活跃的飘字（使其面向摄像机）
    /// </summary>
    public static void LateUpdate()
    {
        if (!s_IsInitialized || s_MainCamera == null) return;

        // 更新摄像机引用（以防切换）
        if (s_MainCamera == null || !s_MainCamera.isActiveAndEnabled)
        {
            s_MainCamera = CameraRegistry.PlayerCamera;
        }

        // 让所有飘字面向摄像机
        foreach (var popup in s_ActivePopups)
        {
            if (popup != null && popup.gameObject.activeInHierarchy)
            {
                popup.FaceCamera(s_MainCamera);
            }
        }
    }

    #endregion
}
