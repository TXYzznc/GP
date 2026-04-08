using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 描边效果测试控制器
/// 用于测试 OutlineController 的各种描边效果
/// </summary>
public class OutlineTestController : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD

    #region 字段

    /// <summary>手动指定的测试目标（可选，为空时自动查找场景中的棋子）</summary>
    [Header("测试目标")]
    [Tooltip("手动拖入测试目标，为空时从场景自动查找")]
    public GameObject ManualTarget;

    /// <summary>当前正在描边的对象列表</summary>
    private List<OutlineController> m_ActiveOutlines = new List<OutlineController>();

    #endregion

    #region 测试方法

    /// <summary>
    /// 对目标显示选中描边（黄色）
    /// </summary>
    public void TestSelectionOutline()
    {
        var controller = GetTargetOutlineController();
        if (controller == null) return;

        ClearAll();
        controller.ShowOutline(OutlineController.SelectionColor, OutlineController.DefaultSize);
        m_ActiveOutlines.Add(controller);
        DebugEx.LogModule("OutlineTest", $"显示选中描边（黄色）: {controller.gameObject.name}");
    }

    /// <summary>
    /// 对目标显示友方描边（绿色）
    /// </summary>
    public void TestAllyOutline()
    {
        var controller = GetTargetOutlineController();
        if (controller == null) return;

        ClearAll();
        controller.ShowOutline(OutlineController.AllyColor, OutlineController.DefaultSize);
        m_ActiveOutlines.Add(controller);
        DebugEx.LogModule("OutlineTest", $"显示友方描边（绿色）: {controller.gameObject.name}");
    }

    /// <summary>
    /// 对目标显示敌方描边（红色）
    /// </summary>
    public void TestEnemyOutline()
    {
        var controller = GetTargetOutlineController();
        if (controller == null) return;

        ClearAll();
        controller.ShowOutline(OutlineController.EnemyColor, OutlineController.DefaultSize);
        m_ActiveOutlines.Add(controller);
        DebugEx.LogModule("OutlineTest", $"显示敌方描边（红色）: {controller.gameObject.name}");
    }

    /// <summary>
    /// 自定义颜色和宽度描边
    /// </summary>
    public void TestCustomOutline(Color color, float size)
    {
        var controller = GetTargetOutlineController();
        if (controller == null) return;

        ClearAll();
        controller.ShowOutline(color, size);
        m_ActiveOutlines.Add(controller);
        DebugEx.LogModule("OutlineTest", $"显示自定义描边: {controller.gameObject.name}, 颜色={color}, 宽度={size}");
    }

    /// <summary>
    /// 对场景中所有玩家棋子显示友方描边
    /// </summary>
    public void TestAllAllyOutlines()
    {
        ClearAll();
        var allChess = FindObjectsOfType<ChessEntity>();
        foreach (var chess in allChess)
        {
            if (chess.Camp == (int)CampType.Player && chess.OutlineController != null)
            {
                chess.OutlineController.ShowOutline(OutlineController.AllyColor, OutlineController.DefaultSize);
                m_ActiveOutlines.Add(chess.OutlineController);
            }
        }
        DebugEx.LogModule("OutlineTest", $"显示所有友方棋子描边，数量={m_ActiveOutlines.Count}");
    }

    /// <summary>
    /// 对场景中所有敌方棋子显示敌方描边
    /// </summary>
    public void TestAllEnemyOutlines()
    {
        ClearAll();
        var allChess = FindObjectsOfType<ChessEntity>();
        foreach (var chess in allChess)
        {
            if (chess.Camp == (int)CampType.Enemy && chess.OutlineController != null)
            {
                chess.OutlineController.ShowOutline(OutlineController.EnemyColor, OutlineController.DefaultSize);
                m_ActiveOutlines.Add(chess.OutlineController);
            }
        }
        DebugEx.LogModule("OutlineTest", $"显示所有敌方棋子描边，数量={m_ActiveOutlines.Count}");
    }

    /// <summary>
    /// 对场景中所有棋子按阵营显示描边（友方绿色 + 敌方红色）
    /// </summary>
    public void TestAllCampOutlines()
    {
        ClearAll();
        var allChess = FindObjectsOfType<ChessEntity>();
        foreach (var chess in allChess)
        {
            if (chess.OutlineController == null) continue;

            Color color = chess.Camp == (int)CampType.Player
                ? OutlineController.AllyColor
                : OutlineController.EnemyColor;
            chess.OutlineController.ShowOutline(color, OutlineController.DefaultSize);
            m_ActiveOutlines.Add(chess.OutlineController);
        }
        DebugEx.LogModule("OutlineTest", $"显示所有棋子阵营描边，数量={m_ActiveOutlines.Count}");
    }

    /// <summary>
    /// 清除所有描边
    /// </summary>
    public void ClearAll()
    {
        foreach (var outline in m_ActiveOutlines)
        {
            if (outline != null)
            {
                outline.HideOutline();
            }
        }
        m_ActiveOutlines.Clear();
        DebugEx.LogModule("OutlineTest", "已清除所有描边");
    }

    /// <summary>
    /// 获取当前活跃描边的数量
    /// </summary>
    public int GetActiveCount()
    {
        // 清理已销毁的
        m_ActiveOutlines.RemoveAll(o => o == null);
        return m_ActiveOutlines.Count;
    }

    #endregion

    #region 内部方法

    /// <summary>
    /// 获取测试目标的 OutlineController
    /// </summary>
    private OutlineController GetTargetOutlineController()
    {
        GameObject target = ManualTarget;

        // 如果没有手动指定，尝试从场景中找第一个玩家棋子
        if (target == null)
        {
            var allChess = FindObjectsOfType<ChessEntity>();
            foreach (var chess in allChess)
            {
                if (chess.Camp == (int)CampType.Player)
                {
                    target = chess.gameObject;
                    break;
                }
            }
        }

        if (target == null)
        {
            DebugEx.WarningModule("OutlineTest", "没有找到测试目标，请手动指定或确保场景中有棋子");
            return null;
        }

        var controller = target.GetComponent<OutlineController>();
        if (controller == null)
        {
            controller = target.GetComponentInChildren<OutlineController>();
        }
        if (controller == null)
        {
            controller = target.AddComponent<OutlineController>();
            DebugEx.LogModule("OutlineTest", $"为 {target.name} 自动添加 OutlineController");
        }

        return controller;
    }

    #endregion

#endif
}
