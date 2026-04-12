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

        // 根据分类显示对应的图标
        Image targetIcon = GetIconByCategory(m_EntryData.Category);
        HideAllIcons();

        if (targetIcon != null)
        {
            targetIcon.gameObject.SetActive(true);

            if (unlocked && m_EntryData.IconId > 0)
            {
                targetIcon.color = Color.white;
                LoadIconAsync(m_EntryData.IconId, targetIcon).Forget();
            }
            else
            {
                // 未解锁或无图标：灰暗显示
                targetIcon.color = new Color(0.15f, 0.15f, 0.2f);
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

    /// <summary>
    /// 根据分类获取对应的图标Image
    /// </summary>
    private Image GetIconByCategory(DictionaryCategory category)
    {
        switch (category)
        {
            case DictionaryCategory.Chess:
                return varChess_Icon;
            case DictionaryCategory.StrategyCard:
                return varCard_Icon;
            case DictionaryCategory.Equipment:
            case DictionaryCategory.Treasure:
            case DictionaryCategory.Consumable:
            case DictionaryCategory.QuestItem:
                return varItem_Icon;
            case DictionaryCategory.Enemy:
                return varOther_Icon;
            default:
                return varOther_Icon;
        }
    }

    /// <summary>
    /// 隐藏所有图标
    /// </summary>
    private void HideAllIcons()
    {
        if (varChess_Icon != null)
            varChess_Icon.gameObject.SetActive(false);
        if (varCard_Icon != null)
            varCard_Icon.gameObject.SetActive(false);
        if (varItem_Icon != null)
            varItem_Icon.gameObject.SetActive(false);
        if (varOther_Icon != null)
            varOther_Icon.gameObject.SetActive(false);
    }

    private async UniTask LoadIconAsync(int iconId, Image targetIcon)
    {
        if (iconId <= 0 || targetIcon == null)
            return;

        try
        {
            await GameExtension.ResourceExtension.LoadSpriteAsync(iconId, targetIcon, 1f, null);
            targetIcon.color = Color.white;
        }
        catch (Exception e)
        {
            DebugEx.ErrorModule(
                "DictionaryItem",
                $"加载图标异常: IconId={iconId}, Error={e.Message}"
            );
        }
    }

    #endregion
}
