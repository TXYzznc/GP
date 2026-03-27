using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人阵型管理器
/// 负责根据敌人数量和阵型类型计算站位
/// </summary>
public static class EnemyFormationManager
{
    #region 阵型类型常量

    public const int FORMATION_HORIZONTAL = 1;  // 横排
    public const int FORMATION_VERTICAL = 2;    // 竖排
    public const int FORMATION_RECTANGLE = 3;   // 矩形  

    #endregion

    #region 公共方法

    /// <summary>
    /// 计算敌人站位
    /// </summary>
    /// <param name="centerPosition">阵型中心点（世界坐标）</param>
    /// <param name="enemyCount">敌人数量</param>
    /// <param name="formationType">阵型类型</param>
    /// <param name="spacing">间距（米）</param>
    /// <returns>站位列表（世界坐标）</returns>
    public static List<Vector3> CalculateFormation(
        Vector3 centerPosition,
        int enemyCount,
        int formationType,
        float spacing)
    {
        if (enemyCount <= 0)
        {
            DebugEx.WarningModule("EnemyFormationManager", "敌人数量为0，返回空列表");
            return new List<Vector3>();
        }

        DebugEx.LogModule("EnemyFormationManager",
            $"计算阵型: 中心={centerPosition}, 数量={enemyCount}, 类型={formationType}, 间距={spacing}");

        List<Vector3> positions = formationType switch
        {
            FORMATION_HORIZONTAL => CalculateHorizontalFormation(centerPosition, enemyCount, spacing),
            FORMATION_VERTICAL => CalculateVerticalFormation(centerPosition, enemyCount, spacing),
            FORMATION_RECTANGLE => CalculateRectangleFormation(centerPosition, enemyCount, spacing),
            _ => CalculateHorizontalFormation(centerPosition, enemyCount, spacing) // 默认横排
        };

        DebugEx.LogModule("EnemyFormationManager",
            $"阵型计算完成，生成 {positions.Count} 个站位");

        return positions;
    }

    #endregion

    #region 私有方法 - 阵型计算

    /// <summary>
    /// 计算横排阵型
    /// </summary>
    private static List<Vector3> CalculateHorizontalFormation(
        Vector3 centerPosition,
        int count,
        float spacing)
    {
        List<Vector3> positions = new List<Vector3>(count);

        // 计算总宽度
        float totalWidth = (count - 1) * spacing;
        float startX = centerPosition.x - totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(
                startX + i * spacing,
                centerPosition.y,
                centerPosition.z
            );
            positions.Add(pos);
        }

        return positions;
    }

    /// <summary>
    /// 计算竖排阵型
    /// </summary>
    private static List<Vector3> CalculateVerticalFormation(
        Vector3 centerPosition,
        int count,
        float spacing)
    {
        List<Vector3> positions = new List<Vector3>(count);

        // 计算总深度
        float totalDepth = (count - 1) * spacing;
        float startZ = centerPosition.z - totalDepth / 2f;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(
                centerPosition.x,
                centerPosition.y,
                startZ + i * spacing
            );
            positions.Add(pos);
        }

        return positions;
    }

    /// <summary>
    /// 计算矩形阵型
    /// 自动计算最接近正方形的行列数
    /// </summary>
    private static List<Vector3> CalculateRectangleFormation(
        Vector3 centerPosition,
        int count,
        float spacing)
    {
        List<Vector3> positions = new List<Vector3>(count);

        // 计算行列数（尽量接近正方形）
        int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
        int rows = Mathf.CeilToInt((float)count / cols);

        DebugEx.LogModule("EnemyFormationManager",
            $"矩形阵型: {rows}行 x {cols}列");

        // 计算起始位置（左上角）
        float totalWidth = (cols - 1) * spacing;
        float totalDepth = (rows - 1) * spacing;
        float startX = centerPosition.x - totalWidth / 2f;
        float startZ = centerPosition.z - totalDepth / 2f;

        // 生成站位
        int index = 0;
        for (int row = 0; row < rows && index < count; row++)
        {
            for (int col = 0; col < cols && index < count; col++)
            {
                Vector3 pos = new Vector3(
                    startX + col * spacing,
                    centerPosition.y,
                    startZ + row * spacing
                );
                positions.Add(pos);
                index++;
            }
        }

        return positions;
    }

    #endregion
}
