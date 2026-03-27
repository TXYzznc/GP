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

    // 使用反射访问私有字段cdRemain
    private System.Reflection.FieldInfo m_CdRemainField;

    #endregion

    #region 初始化

    protected override void OnInit()
    {
        base.OnInit();

        // 初始化时隐藏冷却遮罩UI
        if (varCooldownMask != null)
            varCooldownMask.fillAmount = 0f;

        if (varCooldownText != null)
            varCooldownText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 绑定技能数据
    /// </summary>
    public async void BindSkill(IPlayerSkill skill, SkillCommonConfig config, int slotIndex)
    {
        m_Skill = skill;
        m_SkillConfig = config;
        m_SlotIndex = slotIndex;

        // 设置按键提示（显示对应的按键名称）
        if (varKeyHint != null)
        {
            string keyName = GetKeyNameBySlot(slotIndex);
            varKeyHint.text = keyName;
        }

        // 获取cdRemain字段的反射信息（用于读取冷却时间）
        if (m_Skill != null)
        {
            m_CdRemainField = m_Skill
                .GetType()
                .GetField(
                    "cdRemain",
                    System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance
                );
        }

        // 加载并设置图标
        await LoadIconAsync(config.IconId);

        // 刷新显示
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
        m_CdRemainField = null;

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

        // 获取当前冷却时间
        float cdRemaining = GetCooldownRemaining();
        float cdTotal = m_SkillConfig.Cooldown;

        // 更新冷却显示
        UpdateCooldownDisplay(cdRemaining, cdTotal);
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
            DebugEx.Error(e);
        }
    }

    #endregion

    #region 冷却时间获取

    /// <summary>
    /// 获取技能剩余冷却时间（通过反射）
    /// </summary>
    private float GetCooldownRemaining()
    {
        if (m_Skill == null || m_CdRemainField == null)
            return 0f;

        try
        {
            object value = m_CdRemainField.GetValue(m_Skill);
            if (value is float cdRemain)
            {
                return cdRemain;
            }
        }
        catch (Exception ex)
        {
            DebugEx.Error($"[PlayerSkillSlot] 获取冷却时间失败: {ex.Message}");
        }

        return 0f;
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
