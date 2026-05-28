using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameExtension;
using GameFramework.DataTable;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 宝物卡片 - 在 CharacterBagUI 的宝物仓库中显示或在棋子装备槽位中显示
/// </summary>
public partial class TreasureItemUI : UIItemBase
{
    private int m_TreasureId;
    private int m_TreasureQuantity;
    private Image m_GlowImage;
    private Tween m_GlowPulseTween;

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

        // 异步加载宝物图标
        LoadTreasureImageAsync(treasureRow).Forget();

        // ⭐ 根据稀有度应用发光效果（从ItemTable获取稀有度）
        ApplyRarityGlowAsync(treasureRow).Forget();
    }

    private async UniTaskVoid ApplyRarityGlowAsync(TreasureTable treasureRow)
    {
        // 通过 ItemTableId 从 ItemTable 获取稀有度
        IDataTable<ItemTable> dtItem = GF.DataTable.GetDataTable<ItemTable>();
        ItemTable itemRow = dtItem.GetDataRow(treasureRow.Id);

        if (itemRow == null)
        {
            DebugEx.Warning(
                nameof(TreasureItemUI),
                $"未找到ItemTable数据: ItemTableId={treasureRow.Id}"
            );
            return;
        }

        // 等待一帧，确保UI已经初始化
        await UniTask.Yield();

        ApplyRarityGlow(itemRow.Rarity);
    }

    private async UniTask LoadTreasureImageAsync(TreasureTable treasureRow)
    {
        if (varTreasureImg == null || treasureRow == null)
            return;

        // 通过 ItemTableId 从 ItemTable 获取图标配置ID
        IDataTable<ItemTable> dtItem = GF.DataTable.GetDataTable<ItemTable>();
        ItemTable itemRow = dtItem.GetDataRow(treasureRow.Id);

        if (itemRow == null)
            return;

        // 加载宝物的图标
        await ResourceExtension.LoadSpriteAsync(itemRow.IconId, varTreasureImg);
    }

    private void ApplyRarityGlow(int rarity)
    {
        m_GlowPulseTween?.Kill();
        m_GlowPulseTween = RarityGlowHelper.Apply(transform, rarity, ref m_GlowImage);
    }

    public int GetTreasureId() => m_TreasureId;

    public int GetQuantity() => m_TreasureQuantity;

    /// <summary>
    /// 检查是否有宝物数据
    /// </summary>
    public bool HasItem() => m_TreasureId > 0;

    public void SetQuantity(int quantity)
    {
        m_TreasureQuantity = quantity;
        // 如果有数量文本，可以在这里更新显示
    }

    private void OnDestroy()
    {
        // 清理动画
        m_GlowPulseTween?.Kill();
        m_GlowPulseTween = null;
    }
}
