using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 图鉴条目UI - 类似 InventoryItemUI
/// 放在 DictionarySlot 内，负责显示具体的图标、名称、锁定状态
/// </summary>
public partial class DictionaryItem : UIItemBase
{
    private DictionaryEntryData m_EntryData;
    private bool m_HasData;

    protected override void OnInit()
    {
        base.OnInit();
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(DictionaryEntryData entryData)
    {
        m_EntryData = entryData;
        m_HasData = true;
        gameObject.SetActive(true);

        RefreshDisplay();
    }

    /// <summary>
    /// 清空显示
    /// </summary>
    public void Clear()
    {
        m_HasData = false;
        m_EntryData = default;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 是否有数据
    /// </summary>
    public bool HasData() => m_HasData;

    /// <summary>
    /// 获取条目数据
    /// </summary>
    public DictionaryEntryData GetEntryData() => m_EntryData;

    #region 私有方法

    private void RefreshDisplay()
    {
        bool unlocked = m_EntryData.IsUnlocked;

        // 名称
        if (varNameText != null)
        {
            varNameText.text = unlocked ? m_EntryData.Name : "???";
            varNameText.color = unlocked ? Color.white : new Color(0.4f, 0.4f, 0.5f);
        }

        // 副标题
        if (varSubText != null)
        {
            varSubText.text = unlocked ? m_EntryData.SubText : "";
            varSubText.gameObject.SetActive(unlocked && !string.IsNullOrEmpty(m_EntryData.SubText));
        }

        // 图标
        if (varIcon != null)
        {
            if (unlocked && m_EntryData.IconId > 0)
            {
                varIcon.gameObject.SetActive(true);
                varIcon.color = Color.white;
                LoadIconAsync(m_EntryData.IconId).Forget();
            }
            else
            {
                // 未解锁或无图标：灰暗显示
                varIcon.gameObject.SetActive(true);
                varIcon.color = new Color(0.15f, 0.15f, 0.2f);
            }
        }

        // 品质色条
        if (varQualityBar != null)
        {
            if (unlocked && m_EntryData.Quality > 0)
            {
                varQualityBar.gameObject.SetActive(true);
                varQualityBar.color = RarityColorHelper.GetColor(m_EntryData.Quality);
            }
            else
            {
                varQualityBar.gameObject.SetActive(false);
            }
        }

        // 锁定遮罩
        if (varLockMask != null)
        {
            varLockMask.SetActive(!unlocked);
        }
    }

    private async UniTask LoadIconAsync(int iconId)
    {
        if (iconId <= 0 || varIcon == null) return;

        try
        {
            await GameExtension.ResourceExtension.LoadSpriteAsync(iconId, varIcon, 1f, null);
            varIcon.color = Color.white;
        }
        catch (Exception e)
        {
            DebugEx.Error("DictionaryItem", $"加载图标异常: IconId={iconId}, Error={e.Message}");
        }
    }

    #endregion
}
