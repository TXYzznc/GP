using System;
using Cysharp.Threading.Tasks;
using GameExtension;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家技能槽位UI组件
/// 负责显示单个技能槽位的UI（图标、冷却进度、提示等）
/// </summary>
public partial class PlayerSkillSlot : UIItemBase
{
    #region 私有字段

    private IPlayerSkill m_Skill;
    private SkillCommonConfig m_SkillConfig;
    private int m_SlotIndex;

    // 上帧是否处于冷却中，用于跳过无变化的帧刷新
    private bool m_WasInCooldown;

    #endregion

    #region 初始化

    protected override void OnInit()
    {
        base.OnInit();

        if (varCooldownMask != null)
            varCooldownMask.fillAmount = 0f;

        if (varCooldownText != null)
            varCooldownText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 绑定技能数据
    /// </summary>
    public void BindSkill(IPlayerSkill skill, SkillCommonConfig config, int slotIndex)
    {
        BindSkillAsync(skill, config, slotIndex).Forget();
    }

    private async UniTaskVoid BindSkillAsync(IPlayerSkill skill, SkillCommonConfig config, int slotIndex)
    {
        m_Skill = skill;
        m_SkillConfig = config;
        m_SlotIndex = slotIndex;
        m_WasInCooldown = false;

        if (varKeyHint != null)
            varKeyHint.text = GetKeyNameBySlot(slotIndex);

        await LoadIconAsync(config.IconId);

        RefreshDisplay();
    }

    /// <summary>
    /// 根据槽位索引获取对应的按键名称
    /// </summary>
    private string GetKeyNameBySlot(int slotIndex)
    {
        return slotIndex switch
        {
            1 => "1",
            2 => "2",
            3 => "3",
            _ => slotIndex.ToString(),
        };
    }

    /// <summary>
    /// 清空槽位
    /// </summary>
    public void Clear()
    {
        m_Skill = null;
        m_SkillConfig = default;
        m_WasInCooldown = false;

        // 清空图标
        if (varIcon != null)
        {
            varIcon.sprite = null;
            varIcon.color = new Color(1f, 1f, 1f, 0.3f);
        }

        // 隐藏冷却UI
        if (varCooldownMask != null)
            varCooldownMask.fillAmount = 0f;

        if (varCooldownText != null)
            varCooldownText.gameObject.SetActive(false);
    }

    #endregion

    #region 显示更新

    /// <summary>
    /// 刷新显示（每帧调用）
    /// </summary>
    public void RefreshDisplay()
    {
        if (m_Skill == null)
            return;

        float cdRemaining = m_Skill.CdRemain;
        bool isInCooldown = cdRemaining > 0f;

        // 不在冷却且上帧也不在冷却：无需刷新，跳过避免 Canvas Rebuild
        if (!isInCooldown && !m_WasInCooldown)
            return;

        m_WasInCooldown = isInCooldown;
        UpdateCooldownDisplay(cdRemaining, m_SkillConfig.Cooldown);
    }

    /// <summary>
    /// 更新冷却显示
    /// </summary>
    private void UpdateCooldownDisplay(float remaining, float total)
    {
        bool isInCooldown = remaining > 0f;

        // 更新冷却遮罩
        if (varCooldownMask != null)
        {
            if (total > 0f)
            {
                varCooldownMask.fillAmount = remaining / total;
            }
            else
            {
                varCooldownMask.fillAmount = 0f;
            }
        }

        // 更新冷却文本
        if (varCooldownText != null)
        {
            if (isInCooldown)
            {
                varCooldownText.gameObject.SetActive(true);
                varCooldownText.text = Mathf.CeilToInt(remaining).ToString();
            }
            else
            {
                varCooldownText.gameObject.SetActive(false);
            }
        }
    }

    #endregion

    #region 图标加载

    /// <summary>
    /// 异步加载图标
    /// </summary>
    private async UniTask LoadIconAsync(int iconId)
    {
        if (varIcon == null || iconId <= 0)
            return;

        try
        {
            // 使用ResourceExtension通过配置表ID加载到Image对象
            if (varIcon != null)
            {
                await ResourceExtension.LoadSpriteAsync(iconId, varIcon, 1f, null);
                varIcon.color = Color.white;
            }
        }
        catch (Exception e)
        {
            DebugEx.Error("PlayerSkillSlot", e.Message);
        }
    }

    #endregion

    #region 公共接口

    /// <summary>
    /// 获取技能配置
    /// </summary>
    public SkillCommonConfig GetSkillConfig()
    {
        return m_SkillConfig;
    }

    /// <summary>
    /// 获取槽位索引
    /// </summary>
    public int GetSlotIndex()
    {
        return m_SlotIndex;
    }

    /// <summary>
    /// 是否有技能
    /// </summary>
    public bool HasSkill()
    {
        return m_Skill != null;
    }

    #endregion
}
