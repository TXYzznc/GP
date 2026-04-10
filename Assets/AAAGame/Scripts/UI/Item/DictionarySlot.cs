using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 图鉴格子UI - 类似 InventorySlotUI
/// 作为容器包含一个 DictionaryItem 子对象
/// </summary>
public partial class DictionarySlot : UIItemBase
{
    private DictionaryItem m_ItemUI;
    private DictionaryEntryData m_EntryData;
    private Action<DictionaryEntryData> m_OnClickCallback;

    /// <summary>格子索引</summary>
    public int SlotIndex { get; private set; }

    protected override void OnInit()
    {
        base.OnInit();

        // 查找子对象中的 DictionaryItem
        m_ItemUI = GetComponentInChildren<DictionaryItem>(true);

        if (m_ItemUI == null)
        {
            DebugEx.Warning("DictionarySlot", $"格子 {gameObject.name} 找不到 DictionaryItem 子组件");
        }
    }

    /// <summary>
    /// 设置格子索引
    /// </summary>
    public void SetSlotIndex(int index)
    {
        SlotIndex = index;
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(DictionaryEntryData entryData, Action<DictionaryEntryData> onClickCallback)
    {
        m_EntryData = entryData;
        m_OnClickCallback = onClickCallback;

        // 设置背景颜色（根据品质）
        if (varBg != null)
        {
            if (entryData.IsUnlocked && entryData.Quality > 0)
            {
                var color = RarityColorHelper.GetColor(entryData.Quality);
                color.a = 0.2f; // 格子底色用低透明度
                varBg.color = color;
            }
            else
            {
                varBg.color = RarityColorHelper.DefaultBg;
            }
        }

        // 设置子物体数据
        if (m_ItemUI != null)
        {
            m_ItemUI.SetData(entryData);
        }

        // 绑定点击
        if (varBtn != null)
        {
            varBtn.onClick.RemoveAllListeners();
            varBtn.onClick.AddListener(OnSlotClicked);
        }
    }

    /// <summary>
    /// 清空格子
    /// </summary>
    public void Clear()
    {
        m_EntryData = default;
        m_OnClickCallback = null;

        if (varBg != null)
            varBg.color = RarityColorHelper.DefaultBg;

        if (m_ItemUI != null)
            m_ItemUI.Clear();
    }

    private void OnSlotClicked()
    {
        m_OnClickCallback?.Invoke(m_EntryData);
    }

    public DictionaryItem GetItemUI() => m_ItemUI;
}
