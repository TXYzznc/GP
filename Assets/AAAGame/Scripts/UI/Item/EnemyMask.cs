using Cysharp.Threading.Tasks;
using GameExtension;
using UnityEngine;

public partial class EnemyMask : UIItemBase
{
    #region 私有字段

    private EnemyEntity m_TrackedEnemy;

    #endregion

    #region 公共方法

    public void Setup(EnemyEntity enemy, float alertProgress)
    {
        m_TrackedEnemy = enemy;

        if (m_TrackedEnemy == null)
        {
            DebugEx.Warning(this.GetType().Name, "敌人实体为空");
            return;
        }

        if (varEnemyImg != null && enemy.Config.EnemyIconId > 0)
        {
            ResourceExtension.LoadSpriteAsync(enemy.Config.EnemyIconId, varEnemyImg).Forget();
        }

        if (varWarningSlider != null)
        {
            varWarningSlider.value = alertProgress;
        }

        if (varEnemyName != null)
        {
            varEnemyName.text = m_TrackedEnemy.Config.Name;
        }

        DebugEx.Log(this.GetType().Name, $"设置指示器: {m_TrackedEnemy.Config.Name}, 警觉度={alertProgress:F2}");
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
