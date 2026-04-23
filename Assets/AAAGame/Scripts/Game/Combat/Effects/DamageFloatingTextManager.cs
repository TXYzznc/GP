using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using GameExtension;
using TMPro;

/// <summary>
/// 伤害飘字管理器
/// 负责管理不同类型的伤害飘字配置和显示
/// </summary>
public class DamageFloatingTextManager : SingletonBase<DamageFloatingTextManager>
{
    #region 常量

    private const string WORLD_CANVAS_NAME = "WorldCanvas";

    #endregion

    #region 伤害类型枚举

    /// <summary>
    /// 伤害类型ID枚举
    /// 对应配置表中的ID字段，避免硬编码
    /// </summary>
    public enum DamageType
    {
        普通伤害 = 1,
        暴击伤害 = 2,
        法术伤害 = 3,
        真实伤害 = 4,
        护盾吸收 = 5,
        毒伤害 = 6,
        火焰伤害 = 7,
        冰霜伤害 = 8,
        雷电伤害 = 9,
        治疗效果 = 10,
        护盾获得 = 11,
        经验获得 = 12,
        金币获得 = 13,
        技能伤害 = 14,
        连击伤害 = 15,
        反弹伤害 = 16,
        吸血效果 = 17,
        法力消耗 = 18,
        法力恢复 = 19,
        免疫提示 = 20,
    }

    #endregion

    #region 私有字段

    [Header("对象池设置")]
    [SerializeField]
    private int m_PoolInitialSize = 15; // ⭐ 对象池初始大小

    [SerializeField]
    private int m_PoolMaxSize = 50; // ⭐ 对象池最大容量

    private GameObject m_PopupPrefab;
    private Transform m_PopupParent;
    private DamageFloatingTextAnimationLibrary m_AnimationLibrary;
    private Queue<DamagePopupItem> m_PopupPool;

    #endregion

    #region Unity 生命周期

    protected override void Awake()
    {
        base.Awake();
        DebugEx.LogModule("DamageFloatingTextManager", "管理器初始化开始");

        InitializeManagerAsync().Forget();
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 异步初始化管理器
    /// 从配置表加载预制体和创建对象池
    /// </summary>
    private async UniTaskVoid InitializeManagerAsync()
    {
        // 初始化对象池
        m_PopupPool = new Queue<DamagePopupItem>();

        // 初始化动画库
        m_AnimationLibrary = new DamageFloatingTextAnimationLibrary();

        // 从配置表读取预制体ID
        var config = DataTableExtension.GetRowById<DamageFloatingTextTable>(1);
        if (config == null)
        {
            DebugEx.Error("DamageFloatingTextManager", "未找到飘字配置表");
            return;
        }

        // 加载预制体（从配置表读取EffectId）
        m_PopupPrefab = await ResourceExtension.LoadPrefabAsync(config.EffectId);
        if (m_PopupPrefab == null)
        {
            DebugEx.Error("DamageFloatingTextManager", $"加载飘字预制体失败: EffectId={config.EffectId}");
            return;
        }

        // 查找或创建WorldCanvas，设置为PopupParent
        GameObject worldCanvas = GameObject.Find(WORLD_CANVAS_NAME);
        if (worldCanvas == null)
        {
            worldCanvas = new GameObject(WORLD_CANVAS_NAME);
        }
        m_PopupParent = worldCanvas.transform;

        // ⭐ 对象池预热：预创建指定数量的对象
        PrewarmPool();

        DebugEx.Success("DamageFloatingTextManager", "管理器初始化完成");
    }

    /// <summary>
    /// 对象池预热：预创建对象避免运行时卡顿
    /// </summary>
    private void PrewarmPool()
    {
        if (m_PopupPrefab == null)
        {
            DebugEx.Warning("DamageFloatingTextManager", "飘字预制体未设置，跳过对象池预热");
            return;
        }

        DebugEx.LogModule(
            "DamageFloatingTextManager",
            $"开始对象池预热，目标数量: {m_PoolInitialSize}"
        );

        for (int i = 0; i < m_PoolInitialSize; i++)
        {
            GameObject popupObj = Instantiate(m_PopupPrefab, m_PopupParent);
            DamagePopupItem popupItem = popupObj.GetComponent<DamagePopupItem>();

            if (popupItem != null)
            {
                // 初始化文本组件
                TextMeshProUGUI textComponent = popupObj.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    popupItem.Initialize(textComponent);
                }
                else
                {
                    DebugEx.Error("DamageFloatingTextManager", "飘字预制体缺少 TextMeshProUGUI 组件");
                    Destroy(popupObj);
                    break;
                }

                popupObj.SetActive(false);
                m_PopupPool.Enqueue(popupItem);
            }
            else
            {
                DebugEx.Error("DamageFloatingTextManager", "预制体缺少 DamagePopupItem 组件");
                Destroy(popupObj);
                break;
            }
        }

        DebugEx.Success(
            "DamageFloatingTextManager",
            $"对象池预热完成，已创建 {m_PopupPool.Count} 个对象"
        );
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 显示伤害飘字
    /// </summary>
    /// <param name="damageType">伤害类型</param>
    /// <param name="value">数值</param>
    /// <param name="worldPosition">世界坐标位置</param>
    public void ShowDamageText(DamageType damageType, float value, Vector3 worldPosition)
    {
        ShowDamageText((int)damageType, value.ToString(), worldPosition);
    }

    /// <summary>
    /// 显示伤害飘字（通过ID）
    /// </summary>
    /// <param name="typeId">类型ID</param>
    /// <param name="text">显示文本</param>
    /// <param name="worldPosition">世界坐标位置</param>
    public void ShowDamageText(int typeId, string text, Vector3 worldPosition)
    {
        // 检查是否已初始化
        if (m_PopupPrefab == null)
        {
            DebugEx.Error("DamageFloatingTextManager", "尚未初始化完成，无法显示飘字");
            return;
        }

        // 从配置表获取配置数据
        var config = DataTableExtension.GetRowById<DamageFloatingTextTable>(typeId);
        if (config == null)
        {
            DebugEx.Error("DamageFloatingTextManager", $"未找到类型ID {typeId} 的配置");
            return;
        }

        // 从对象池获取或创建飘字对象
        DamagePopupItem popupItem = GetPopupFromPool();
        if (popupItem == null)
        {
            DebugEx.Error("DamageFloatingTextManager", "无法创建飘字对象");
            return;
        }

        // 应用配置
        ApplyConfig(popupItem, config, text, worldPosition);
    }

    /// <summary>
    /// 显示伤害飘字（简化版：仅需提供世界位置、伤害值和伤害类型）
    /// 用于从 CombatVFXManager 迁移
    /// </summary>
    /// <param name="worldPosition">世界坐标位置</param>
    /// <param name="damage">伤害值</param>
    /// <param name="damageTypeName">伤害类型名称（对应 DamageType 枚举）</param>
    public void ShowDamageTextSimple(Vector3 worldPosition, double damage, string damageTypeName)
    {
        // 将伤害类型名称转换为 DamageType 枚举值
        if (!System.Enum.TryParse<DamageType>(damageTypeName, out var damageType))
        {
            DebugEx.Warning("DamageFloatingTextManager", $"无效的伤害类型: {damageTypeName}");
            damageType = DamageType.普通伤害;
        }

        ShowDamageText(damageType, (float)damage, worldPosition);
    }

    /// <summary>
    /// 获取配置信息
    /// </summary>
    /// <param name="damageType">伤害类型</param>
    /// <returns>配置信息，如果不存在返回null</returns>
    public DamageFloatingTextTable GetConfig(DamageType damageType)
    {
        return GetConfig((int)damageType);
    }

    /// <summary>
    /// 获取配置信息（通过ID）
    /// </summary>
    /// <param name="typeId">类型ID</param>
    /// <returns>配置信息，如果不存在返回null</returns>
    public DamageFloatingTextTable GetConfig(int typeId)
    {
        return DataTableExtension.GetRowById<DamageFloatingTextTable>(typeId);
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 从对象池获取飘字对象
    /// </summary>
    private DamagePopupItem GetPopupFromPool()
    {
        // 尝试从池中获取
        if (m_PopupPool.Count > 0)
        {
            DamagePopupItem popupItem = m_PopupPool.Dequeue();
            popupItem.gameObject.SetActive(true);
            return popupItem;
        }

        // 池中没有可用对象，创建新的
        if (m_PopupPrefab != null)
        {
            DebugEx.Warning("DamageFloatingTextManager", "对象池已空，动态创建新对象");

            GameObject popupObj = Instantiate(m_PopupPrefab, m_PopupParent);
            DamagePopupItem popupItem = popupObj.GetComponent<DamagePopupItem>();

            if (popupItem == null)
            {
                DebugEx.Error("DamageFloatingTextManager", "飘字预制体缺少DamagePopupItem组件");
                Destroy(popupObj);
                return null;
            }

            // 初始化文本组件
            TextMeshProUGUI textComponent = popupObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                popupItem.Initialize(textComponent);
            }

            return popupItem;
        }

        DebugEx.Error("DamageFloatingTextManager", "飘字预制体未设置");
        return null;
    }

    /// <summary>
    /// 应用配置到飘字对象
    /// </summary>
    private void ApplyConfig(
        DamagePopupItem popupItem,
        DamageFloatingTextTable config,
        string text,
        Vector3 worldPosition
    )
    {
        // 设置基本属性
        popupItem.SetText(text);
        popupItem.SetFontSize(config.FontSize);
        popupItem.SetScale(config.ScaleMultiplier);
        popupItem.SetPosition(worldPosition);

        // 解析颜色字符串
        Color textColor = ParseColor(config.TextColor);
        Color outlineColor = ParseColor(config.OutlineColor);

        // 设置颜色和样式
        if (config.IsGradient)
        {
            // 使用渐变色
            Color gradientStart = ParseColor(config.GradientStartColor);
            Color gradientEnd = ParseColor(config.GradientEndColor);

            Color[] gradientColors = new Color[]
            {
                gradientStart, // 左下
                gradientStart, // 左上
                gradientEnd, // 右上
                gradientEnd, // 右下
            };
            popupItem.SetTextStyle(true, true, gradientColors, outlineColor);
        }
        else
        {
            // 使用单色
            popupItem.SetTextStyle(true, false, null, outlineColor);
            popupItem.SetColor(textColor);
        }

        // 播放动画，使用动画库（传入配置参数）
        m_AnimationLibrary.PlayAnimation(
            popupItem,
            config.AnimationType,
            config.MoveDistance,
            config.Duration,
            () =>
            {
                // 动画完成后回收到对象池
                ReturnPopupToPool(popupItem);
            }
        );
    }

    /// <summary>
    /// 解析颜色字符串
    /// </summary>
    /// <param name="colorStr">颜色字符串（支持#RRGGBB格式）</param>
    /// <returns>Unity Color对象</returns>
    private Color ParseColor(string colorStr)
    {
        if (string.IsNullOrEmpty(colorStr))
        {
            return Color.white;
        }

        // 尝试解析HTML颜色格式
        if (ColorUtility.TryParseHtmlString(colorStr, out Color color))
        {
            return color;
        }

        // 如果解析失败，返回白色并记录警告
        DebugEx.Warning("DamageFloatingTextManager", $"无法解析颜色: {colorStr}，使用默认白色");
        return Color.white;
    }

    /// <summary>
    /// 将飘字对象回收到对象池
    /// </summary>
    private void ReturnPopupToPool(DamagePopupItem popupItem)
    {
        if (popupItem == null)
            return;

        // ⭐ 容量控制：如果池已满，直接销毁对象
        if (m_PopupPool.Count >= m_PoolMaxSize)
        {
            DebugEx.Warning(
                "DamageFloatingTextManager",
                $"对象池已达最大容量 {m_PoolMaxSize}，销毁多余对象"
            );
            Destroy(popupItem.gameObject);
            return;
        }

        popupItem.gameObject.SetActive(false);
        m_PopupPool.Enqueue(popupItem);
    }

    #endregion
}
