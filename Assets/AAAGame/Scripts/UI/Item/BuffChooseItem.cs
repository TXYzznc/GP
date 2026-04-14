using System;
using Cysharp.Threading.Tasks;
using GameExtension;
using UnityEngine;
using UnityEngine.UI;

public partial class BuffChooseItem : UIItemBase
{
    #region 私有字段

    /// <summary>效果 ID（SpecialEffectTable）</summary>
    private int m_EffectId;

    /// <summary>Buff ID（BuffTable，仅旧接口使用）</summary>
    private int m_BuffId;

    /// <summary>Buff配置</summary>
    private BuffTable m_BuffConfig;

    #endregion

    #region 公共属性

    /// <summary>效果 ID</summary>
    public int EffectId => m_EffectId;

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置 SpecialEffect 数据（先手Buff/偷袭Debuff三选一）
    /// </summary>
    public void SetEffectData(int effectId, SpecialEffectTable effectConfig)
    {
        m_EffectId = effectId;

        if (effectConfig == null)
        {
            DebugEx.WarningModule("BuffChooseItem", $"效果配置为空: ID={effectId}");
            return;
        }

        if (varImg != null && effectConfig.IconId > 0)
        {
            _ = ResourceExtension.LoadSpriteAsync(effectConfig.IconId, varImg);
        }

        if (varBuffName != null)
        {
            varBuffName.text = effectConfig.Name;
        }

        if (varDesc != null)
        {
            varDesc.text = effectConfig.Description;
        }

        // 注册按钮点击事件
        if (varBtn != null)
        {
            varBtn.onClick.RemoveAllListeners();
            varBtn.onClick.AddListener(OnBuffSelected);
        }

        DebugEx.LogModule("BuffChooseItem", $"设置效果数据: {effectConfig.Name} (ID={effectId})");
    }

    /// <summary>
    /// 设置Buff数据（旧接口，保留兼容）
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
            varBtn.onClick.RemoveAllListeners();
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
        // 优先使用 effectId，如果为 0 则使用 buffId
        int selectedId = m_EffectId > 0 ? m_EffectId : m_BuffId;
        DebugEx.LogModule("BuffChooseItem", $"选中效果: ID={selectedId}");

        // 获取父UI（CombatPreparationUI）并通知选择
        Transform parentTransform = transform.parent;
        while (parentTransform != null)
        {
            CombatPreparationUI parentUI = parentTransform.GetComponent<CombatPreparationUI>();
            if (parentUI != null)
            {
                parentUI.OnBuffItemSelected(selectedId);
                return;
            }
            parentTransform = parentTransform.parent;
        }

        DebugEx.WarningModule("BuffChooseItem", "未找到父UI: CombatPreparationUI");
    }

    #endregion
}