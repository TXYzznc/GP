using UnityEngine;

/// <summary>
/// Layer辅助类 - 统一管理项目中的Layer
/// 避免硬编码Layer名称和索引
/// </summary>
public static class LayerHelper
{
    #region Layer 定义

    /// <summary>
    /// Layer索引枚举
    /// </summary>
    public enum Layer
    {
        Default = 0,
        TransparentFX = 1,
        IgnoreRaycast = 2,
        WorldUI = 3,
        Water = 4,
        UI = 5,
        Player = 6,
        Enemy = 7,
        Ally = 8,
        Interactive = 9,
        UI3DModel = 10,
        EnvCollider = 11,
        PlacementPlane = 12,
        Projectile = 13,
        Chess = 14,
        OutlineLayer = 31
    }

    #endregion

    #region LayerMask 快速访问

    /// <summary>
    /// 根据Layer枚举获取LayerMask
    /// </summary>
    public static LayerMask GetMask(Layer layer)
    {
        return 1 << (int)layer;
    }

    /// <summary>
    /// 根据多个Layer枚举获取组合LayerMask
    /// </summary>
    public static LayerMask GetMask(params Layer[] layers)
    {
        int mask = 0;
        foreach (var layer in layers)
        {
            mask |= (1 << (int)layer);
        }
        return mask;
    }

    /// <summary>
    /// 获取Layer名称
    /// </summary>
    public static string GetName(Layer layer)
    {
        return LayerMask.LayerToName((int)layer);
    }

    #endregion

    #region 预设 LayerMask 组合

    /// <summary>
    /// 遮挡检测LayerMask（相机碰撞检测用）
    /// 包含：Default, WorldUI, EnvCollider, Interactive
    /// </summary>
    public static LayerMask OcclusionMask => GetMask(
        Layer.Default,
        Layer.EnvCollider,
        Layer.Interactive
    );

    /// <summary>
    /// 战斗单位LayerMask
    /// 包含：Player, Enemy, Ally, Chess
    /// </summary>
    public static LayerMask CombatUnitMask => GetMask(
        Layer.Player,
        Layer.Enemy,
        Layer.Ally,
        Layer.Chess
    );

    /// <summary>
    /// 敌对单位LayerMask（玩家视角）
    /// 包含：Enemy
    /// </summary>
    public static LayerMask EnemyMask => GetMask(Layer.Enemy);

    /// <summary>
    /// 友方单位LayerMask（玩家视角）
    /// 包含：Player, Ally
    /// </summary>
    public static LayerMask AllyMask => GetMask(Layer.Player, Layer.Ally);

    /// <summary>
    /// 投射物LayerMask
    /// </summary>
    public static LayerMask ProjectileMask => GetMask(Layer.Projectile);

    /// <summary>
    /// 可交互物体LayerMask
    /// </summary>
    public static LayerMask InteractiveMask => GetMask(Layer.Interactive);

    /// <summary>
    /// 棋子放置平面LayerMask
    /// </summary>
    public static LayerMask PlacementMask => GetMask(Layer.PlacementPlane);

    /// <summary>
    /// UI相关LayerMask
    /// 包含：UI, WorldUI, UI3DModel
    /// </summary>
    public static LayerMask UIMask => GetMask(
        Layer.UI,
        Layer.WorldUI,
        Layer.UI3DModel
    );

    #endregion

    #region GameObject Layer 操作

    /// <summary>
    /// 设置GameObject的Layer（递归设置所有子物体）
    /// </summary>
    public static void SetLayerRecursively(GameObject obj, Layer layer)
    {
        if (obj == null) return;

        int layerIndex = (int)layer;
        SetLayerRecursivelyInternal(obj, layerIndex);
    }

    /// <summary>
    /// 内部递归方法
    /// </summary>
    private static void SetLayerRecursivelyInternal(GameObject obj, int layerIndex)
    {
        obj.layer = layerIndex;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursivelyInternal(child.gameObject, layerIndex);
        }
    }

    /// <summary>
    /// 检查GameObject是否在指定Layer
    /// </summary>
    public static bool IsInLayer(GameObject obj, Layer layer)
    {
        return obj != null && obj.layer == (int)layer;
    }

    /// <summary>
    /// 检查GameObject是否在指定LayerMask中
    /// </summary>
    public static bool IsInLayerMask(GameObject obj, LayerMask mask)
    {
        return obj != null && ((1 << obj.layer) & mask) != 0;
    }

    #endregion

    #region 调试方法

    /// <summary>
    /// 打印所有Layer信息
    /// </summary>
    public static void PrintAllLayers()
    {
        DebugEx.LogModule("LayerHelper", "========== Unity Layer 配置 ==========");
        
        foreach (Layer layer in System.Enum.GetValues(typeof(Layer)))
        {
            int index = (int)layer;
            string name = LayerMask.LayerToName(index);
            
            // 检查Layer是否在Unity中配置
            if (!string.IsNullOrEmpty(name))
            {
                DebugEx.LogModule("LayerHelper", $"Layer {index}: {layer} -> Unity名称: {name}");
            }
            else
            {
                DebugEx.WarningModule("LayerHelper", $"Layer {index}: {layer} -> Unity中未配置！");
            }
        }
        
        DebugEx.LogModule("LayerHelper", "====================================");
    }

    /// <summary>
    /// 验证Layer配置是否与Unity一致
    /// </summary>
    public static bool ValidateLayerConfiguration()
    {
        bool isValid = true;
        
        foreach (Layer layer in System.Enum.GetValues(typeof(Layer)))
        {
            int index = (int)layer;
            string unityName = LayerMask.LayerToName(index);
            string enumName = layer.ToString();
            
            if (string.IsNullOrEmpty(unityName))
            {
                DebugEx.ErrorModule("LayerHelper", $"Layer {enumName} (索引{index}) 在Unity中未配置！");
                isValid = false;
            }
            else if (unityName != enumName)
            {
                DebugEx.WarningModule("LayerHelper", 
                    $"Layer名称不匹配: 枚举={enumName}, Unity={unityName} (索引{index})");
            }
        }
        
        return isValid;
    }

    #endregion
}
