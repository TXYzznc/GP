using System;
using Cysharp.Threading.Tasks;
using GameExtension;
using UnityEngine;
using UnityEngine.UI;

public partial class BuffChooseItem : UIItemBase
{
    #region 私有字段

    /// <summary>Buff ID</summary>
    private int m_BuffId;

    /// <summary>Buff配置</summary>
    private BuffTable m_BuffConfig;

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置Buff数据
    /// </summary>
    public void SetBuffData(int buffId, BuffTable buffConfig)
    {
        m_BuffId = buffId;
        m_BuffConfig = buffConfig;

        if (buffConfig == null)
        {
            DebugEx.WarningModule("BuffChooseItem", $"Buff配置为空: ID={buffId}");
            return;
        }

        // 更新UI显示
        if (varImg != null)
        {
            // TODO: 从资源管理器加载Buff图标
            // varImg.sprite = LoadSprite(buffConfig.SpriteId);
        }

        if (varBuffName != null)
        {
            varBuffName.text = buffConfig.Name;
        }

        if (varDesc != null)
        {
            varDesc.text = buffConfig.Desc;
        }

        // 注册按钮点击事件
        if (varBtn != null)
        {
            varBtn.onClick.AddListener(OnBuffSelected);
        }

        DebugEx.LogModule("BuffChooseItem", $"设置Buff数据: {buffConfig.Name}");
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// Buff被选中
    /// </summary>
    private void OnBuffSelected()
    {
        if (m_BuffConfig != null)
        {
            DebugEx.LogModule("BuffChooseItem", $"选中Buff: ID={m_BuffId}, Name={m_BuffConfig.Name}");
        }

        // 获取父UI（CombatPreparationUI）并通知选择
        Transform parentTransform = transform.parent;
        while (parentTransform != null)
        {
            CombatPreparationUI parentUI = parentTransform.GetComponent<CombatPreparationUI>();
            if (parentUI != null)
            {
                parentUI.OnBuffItemSelected(m_BuffId);
                return;
            }
            parentTransform = parentTransform.parent;
        }

        DebugEx.WarningModule("BuffChooseItem", "未找到父UI: CombatPreparationUI");
    }

    #endregion
}