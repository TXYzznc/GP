using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameExtension;
using GameFramework.DataTable;
using Cysharp.Threading.Tasks;

/// <summary>
/// 宝物卡片 - 在 CharacterBagUI 的宝物仓库中显示或在棋子装备槽位中显示
/// </summary>
public partial class TreasureItemUI : UIItemBase
{
    private int m_TreasureId;
    private int m_TreasureQuantity;

    public void InitTreasure(int treasureId, int quantity = 1)
    {
        m_TreasureId = treasureId;
        m_TreasureQuantity = quantity;

        // 从 TreasureTable 获取数据
        IDataTable<TreasureTable> dtTreasure = GF.DataTable.GetDataTable<TreasureTable>();
        TreasureTable treasureRow = dtTreasure.GetDataRow(treasureId);

        if (treasureRow == null)
        {
            DebugEx.Error(nameof(TreasureItemUI), $"宝物 {treasureId} 不存在");
            return;
        }

        // 显示宝物名称
        if (varNameText != null)
        {
            varNameText.text = treasureRow.Name;
        }

        // 显示高亮效果为隐藏状态（交互时显示）
        if (varHighlightImage != null)
        {
            varHighlightImage.gameObject.SetActive(false);
        }

        // 异步加载宝物图标
        LoadTreasureImageAsync(treasureRow).Forget();
    }

    private async UniTask LoadTreasureImageAsync(TreasureTable treasureRow)
    {
        if (varTreasureImg == null || treasureRow == null)
            return;

        // 通过 ItemTableId 从 ItemTable 获取图标配置ID
        IDataTable<ItemTable> dtItem = GF.DataTable.GetDataTable<ItemTable>();
        ItemTable itemRow = dtItem.GetDataRow(treasureRow.ItemTableId);

        if (itemRow == null)
            return;

        // 加载宝物的图标
        await ResourceExtension.LoadSpriteAsync(itemRow.IconId, varTreasureImg);
    }

    public void ShowHighlight(bool show)
    {
        if (varHighlightImage != null)
        {
            varHighlightImage.gameObject.SetActive(show);
        }
    }

    public int GetTreasureId() => m_TreasureId;
    public int GetQuantity() => m_TreasureQuantity;

    public void SetQuantity(int quantity)
    {
        m_TreasureQuantity = quantity;
        // 如果有数量文本，可以在这里更新显示
    }
}
