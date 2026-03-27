using UnityEngine;

/// <summary>
/// 实体坐标获取工具类
/// 提供统一的位置获取方法，支持通用 GameObject/Transform
/// </summary>
public static class EntityPositionHelper
{
    #region 常量

    /// <summary>默认高度偏移（当没有 Renderer/Collider 时使用）</summary>
    private const float DEFAULT_HEIGHT_OFFSET = 1f;

    #endregion

    #region 核心方法 - 支持 GameObject

    /// <summary>
    /// 获取对象的中心位置（用于瞄准、碰撞检测等）
    /// 优先级：Renderer.bounds.center > Collider.bounds.center > Transform.position + 默认偏移
    /// </summary>
    /// <param name="obj">目标对象</param>
    /// <param name="enableLog">是否启用调试日志</param>
    /// <returns>对象中心位置</returns>
    public static Vector3 GetCenterPosition(GameObject obj, bool enableLog = false)
    {
        if (obj == null)
        {
            if (enableLog)
            {
                //DebugEx.WarningModule("EntityPositionHelper", "GetCenterPosition: obj 为 null");
            }
            return Vector3.zero;
        }

        // 优先使用 Renderer 的包围盒中心
        Renderer renderer = obj.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Vector3 center = renderer.bounds.center;
            if (enableLog)
            {
                //DebugEx.LogModule("EntityPositionHelper",$"GetCenterPosition: {obj.name} 使用 Renderer.bounds.center = {center}");
            }
            return center;
        }

        // 其次使用 Collider 的包围盒中心
        Collider collider = obj.GetComponentInChildren<Collider>();
        if (collider != null)
        {
            Vector3 center = collider.bounds.center;
            if (enableLog)
            {
                //DebugEx.LogModule("EntityPositionHelper",$"GetCenterPosition: {obj.name} 使用 Collider.bounds.center = {center}");
            }
            return center;
        }

        // 最后使用固定偏移（默认 1 米高度）
        Vector3 fallbackCenter = obj.transform.position + Vector3.up * DEFAULT_HEIGHT_OFFSET;
        if (enableLog)
        {
            //DebugEx.LogModule("EntityPositionHelper",$"GetCenterPosition: {obj.name} 使用默认偏移 = {fallbackCenter}");
        }
        return fallbackCenter;
    }

    /// <summary>
    /// 获取对象的底部位置（模型实际底部）
    /// 优先级：Renderer.bounds.min.y > Collider.bounds.min.y > Transform.position
    /// </summary>
    /// <param name="obj">目标对象</param>
    /// <param name="enableLog">是否启用调试日志</param>
    /// <returns>对象底部位置</returns>
    public static Vector3 GetBottomPosition(GameObject obj, bool enableLog = false)
    {
        if (obj == null)
        {
            if (enableLog)
            {
                DebugEx.WarningModule("EntityPositionHelper", "GetBottomPosition: obj 为 null");
            }
            return Vector3.zero;
        }

        // 优先使用 Renderer 的包围盒底部
        Renderer renderer = obj.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Vector3 bottomPos = obj.transform.position;
            bottomPos.y = renderer.bounds.min.y;
            if (enableLog)
            {
                DebugEx.LogModule("EntityPositionHelper",
                    $"GetBottomPosition: {obj.name} 使用 Renderer.bounds.min.y = {bottomPos}");
            }
            return bottomPos;
        }

        // 其次使用 Collider 的包围盒底部
        Collider collider = obj.GetComponentInChildren<Collider>();
        if (collider != null)
        {
            Vector3 bottomPos = obj.transform.position;
            bottomPos.y = collider.bounds.min.y;
            if (enableLog)
            {
                DebugEx.LogModule("EntityPositionHelper",
                    $"GetBottomPosition: {obj.name} 使用 Collider.bounds.min.y = {bottomPos}");
            }
            return bottomPos;
        }

        // 最后使用 Transform.position（假设锚点在底部）
        if (enableLog)
        {
            DebugEx.LogModule("EntityPositionHelper",
                $"GetBottomPosition: {obj.name} 使用 Transform.position = {obj.transform.position}");
        }
        return obj.transform.position;
    }

    /// <summary>
    /// 获取对象的顶部位置（用于头顶特效、血条等）
    /// </summary>
    /// <param name="obj">目标对象</param>
    /// <param name="enableLog">是否启用调试日志</param>
    /// <returns>对象顶部位置</returns>
    public static Vector3 GetTopPosition(GameObject obj, bool enableLog = false)
    {
        if (obj == null)
        {
            if (enableLog)
            {
                DebugEx.WarningModule("EntityPositionHelper", "GetTopPosition: obj 为 null");
            }
            return Vector3.zero;
        }

        // 优先使用 Renderer 的包围盒顶部
        Renderer renderer = obj.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Vector3 top = new Vector3(
                obj.transform.position.x,
                renderer.bounds.max.y,
                obj.transform.position.z
            );
            if (enableLog)
            {
                DebugEx.LogModule("EntityPositionHelper",
                    $"GetTopPosition: {obj.name} 使用 Renderer.bounds.max.y = {top}");
            }
            return top;
        }

        // 其次使用 Collider 的包围盒顶部
        Collider collider = obj.GetComponentInChildren<Collider>();
        if (collider != null)
        {
            Vector3 top = new Vector3(
                obj.transform.position.x,
                collider.bounds.max.y,
                obj.transform.position.z
            );
            if (enableLog)
            {
                DebugEx.LogModule("EntityPositionHelper",
                    $"GetTopPosition: {obj.name} 使用 Collider.bounds.max.y = {top}");
            }
            return top;
        }

        // 最后使用固定偏移（默认 2 米高度）
        Vector3 fallbackTop = obj.transform.position + Vector3.up * (DEFAULT_HEIGHT_OFFSET * 2f);
        if (enableLog)
        {
            DebugEx.LogModule("EntityPositionHelper",
                $"GetTopPosition: {obj.name} 使用默认偏移 = {fallbackTop}");
        }
        return fallbackTop;
    }

    /// <summary>
    /// 按比例获取对象位置（0=底部，1=顶部）
    /// 这是最灵活的方法，适用于各种场景
    /// </summary>
    /// <param name="obj">目标对象</param>
    /// <param name="ratio">高度比例（0-1），0=底部，0.5=中心，1=顶部</param>
    /// <param name="enableLog">是否启用调试日志</param>
    /// <returns>指定比例的位置</returns>
    public static Vector3 GetPositionAtRatio(GameObject obj, float ratio, bool enableLog = false)
    {
        if (obj == null)
        {
            if (enableLog)
            {
                DebugEx.WarningModule("EntityPositionHelper", "GetPositionAtRatio: obj 为 null");
            }
            return Vector3.zero;
        }

        ratio = Mathf.Clamp01(ratio);

        Vector3 bottomPos = GetBottomPosition(obj, false);
        Vector3 topPos = GetTopPosition(obj, false);

        Vector3 result = Vector3.Lerp(bottomPos, topPos, ratio);

        if (enableLog)
        {
            DebugEx.LogModule("EntityPositionHelper",
                $"GetPositionAtRatio: {obj.name} ratio={ratio:F2}, 底部Y={bottomPos.y:F2}, 顶部Y={topPos.y:F2}, 结果Y={result.y:F2}");
        }

        return result;
    }

    /// <summary>
    /// 获取对象指定高度的位置（相对于底部）
    /// </summary>
    /// <param name="obj">目标对象</param>
    /// <param name="heightOffset">高度偏移（相对于底部，单位：米）</param>
    /// <returns>指定高度的位置</returns>
    public static Vector3 GetPositionAtHeight(GameObject obj, float heightOffset)
    {
        if (obj == null)
        {
            return Vector3.zero;
        }

        Vector3 bottomPos = GetBottomPosition(obj, false);
        return bottomPos + Vector3.up * heightOffset;
    }

    /// <summary>
    /// 获取对象的包围盒（用于范围检测、碰撞计算等）
    /// </summary>
    /// <param name="obj">目标对象</param>
    /// <returns>对象包围盒，如果无法获取则返回 null</returns>
    public static Bounds? GetBounds(GameObject obj)
    {
        if (obj == null)
        {
            return null;
        }

        // 优先使用 Renderer 的包围盒
        Renderer renderer = obj.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }

        // 其次使用 Collider 的包围盒
        Collider collider = obj.GetComponentInChildren<Collider>();
        if (collider != null)
        {
            return collider.bounds;
        }

        // 无法获取包围盒
        return null;
    }

    /// <summary>
    /// 计算底部偏移量（用于棋子放置时的底部对齐）
    /// 返回从 transform.position 到模型底部的偏移量
    /// </summary>
    /// <param name="obj">目标对象</param>
    /// <returns>底部偏移量（Y轴）</returns>
    public static float CalculateBottomOffset(GameObject obj)
    {
        if (obj == null)
        {
            return 0f;
        }

        Vector3 bottomPos = GetBottomPosition(obj, false);
        return obj.transform.position.y - bottomPos.y;
    }

    #endregion

    #region 重载方法 - 支持 Transform

    /// <summary>
    /// 获取 Transform 的中心位置
    /// </summary>
    public static Vector3 GetCenterPosition(Transform transform, bool enableLog = false)
    {
        return transform != null ? GetCenterPosition(transform.gameObject, enableLog) : Vector3.zero;
    }

    /// <summary>
    /// 获取 Transform 的底部位置
    /// </summary>
    public static Vector3 GetBottomPosition(Transform transform, bool enableLog = false)
    {
        return transform != null ? GetBottomPosition(transform.gameObject, enableLog) : Vector3.zero;
    }

    /// <summary>
    /// 获取 Transform 的顶部位置
    /// </summary>
    public static Vector3 GetTopPosition(Transform transform, bool enableLog = false)
    {
        return transform != null ? GetTopPosition(transform.gameObject, enableLog) : Vector3.zero;
    }

    /// <summary>
    /// 按比例获取 Transform 位置
    /// </summary>
    public static Vector3 GetPositionAtRatio(Transform transform, float ratio, bool enableLog = false)
    {
        return transform != null ? GetPositionAtRatio(transform.gameObject, ratio, enableLog) : Vector3.zero;
    }

    /// <summary>
    /// 获取 Transform 指定高度的位置
    /// </summary>
    public static Vector3 GetPositionAtHeight(Transform transform, float heightOffset)
    {
        return transform != null ? GetPositionAtHeight(transform.gameObject, heightOffset) : Vector3.zero;
    }

    /// <summary>
    /// 获取 Transform 的包围盒
    /// </summary>
    public static Bounds? GetBounds(Transform transform)
    {
        return transform != null ? GetBounds(transform.gameObject) : null;
    }

    /// <summary>
    /// 计算 Transform 的底部偏移量
    /// </summary>
    public static float CalculateBottomOffset(Transform transform)
    {
        return transform != null ? CalculateBottomOffset(transform.gameObject) : 0f;
    }

    #endregion

    #region 重载方法 - 支持 ChessEntity

    /// <summary>
    /// 获取 ChessEntity 的中心位置
    /// </summary>
    public static Vector3 GetCenterPosition(ChessEntity entity, bool enableLog = false)
    {
        return entity != null ? GetCenterPosition(entity.gameObject, enableLog) : Vector3.zero;
    }

    /// <summary>
    /// 获取 ChessEntity 的底部位置
    /// </summary>
    public static Vector3 GetBottomPosition(ChessEntity entity, bool enableLog = false)
    {
        return entity != null ? GetBottomPosition(entity.gameObject, enableLog) : Vector3.zero;
    }

    /// <summary>
    /// 获取 ChessEntity 的顶部位置
    /// </summary>
    public static Vector3 GetTopPosition(ChessEntity entity, bool enableLog = false)
    {
        return entity != null ? GetTopPosition(entity.gameObject, enableLog) : Vector3.zero;
    }

    /// <summary>
    /// 按比例获取 ChessEntity 位置
    /// </summary>
    public static Vector3 GetPositionAtRatio(ChessEntity entity, float ratio, bool enableLog = false)
    {
        return entity != null ? GetPositionAtRatio(entity.gameObject, ratio, enableLog) : Vector3.zero;
    }

    /// <summary>
    /// 获取 ChessEntity 指定高度的位置
    /// </summary>
    public static Vector3 GetPositionAtHeight(ChessEntity entity, float heightOffset)
    {
        return entity != null ? GetPositionAtHeight(entity.gameObject, heightOffset) : Vector3.zero;
    }

    /// <summary>
    /// 获取 ChessEntity 的包围盒
    /// </summary>
    public static Bounds? GetBounds(ChessEntity entity)
    {
        return entity != null ? GetBounds(entity.gameObject) : null;
    }

    /// <summary>
    /// 计算 ChessEntity 的底部偏移量
    /// </summary>
    public static float CalculateBottomOffset(ChessEntity entity)
    {
        return entity != null ? CalculateBottomOffset(entity.gameObject) : 0f;
    }

    #endregion
}
