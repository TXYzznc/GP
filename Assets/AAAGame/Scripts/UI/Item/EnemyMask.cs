using UnityEngine;

/// <summary>
/// 敌人警示指示器
/// 显示单个敌人的头像和警觉度进度条
///
/// 需要的UI变量（需要用户创建预制体）：
/// - varEnemyIcon (Image) - 敌人头像
/// - varAlertProgress (Slider) - 警觉度进度条
/// - varEnemyName (Text, 可选) - 敌人名称
/// - varDistanceText (Text, 可选) - 距离显示
/// </summary>
public partial class EnemyMask : UIItemBase
{
    #region 私有字段

    /// <summary>所追踪的敌人实体</summary>
    private EnemyEntity m_TrackedEnemy;

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置指示器数据
    /// </summary>
    public void Setup(EnemyEntity enemy, Sprite icon, float alertProgress)
    {
        m_TrackedEnemy = enemy;

        if (m_TrackedEnemy == null)
        {
            DebugEx.WarningModule("EnemyAlertIndicator", "敌人实体为空");
            return;
        }

        // 设置敌人头像
        if (varEnemyImg != null && icon != null)
        {
            varEnemyImg.sprite = icon;
        }

        // 设置警觉度进度条
        if (varWarningSlider != null)
        {
            varWarningSlider.value = alertProgress;
        }

        // 设置敌人名称（可选）
        if (varEnemyName != null)
        {
            varEnemyName.text = m_TrackedEnemy.Config.Name;
        }

        DebugEx.LogModule("EnemyAlertIndicator", $"设置指示器: {m_TrackedEnemy.Config.Name}, 警觉度={alertProgress:F2}");
    }

    /// <summary>
    /// 更新警觉度进度条
    /// </summary>
    public void UpdateProgress(float alertProgress)
    {
        if (varWarningSlider != null)
        {
            varWarningSlider.value = alertProgress;
        }

        // 更新距离显示（可选）
        if (varDistanceText != null && m_TrackedEnemy != null)
        {
            Transform playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (playerTransform != null)
            {
                float distance = Vector3.Distance(playerTransform.position, m_TrackedEnemy.transform.position);
                varDistanceText.text = $"{distance:F1}m";
            }
        }
    }

    #endregion
}
