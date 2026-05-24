using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameExtension;
using GameFramework.DataTable;
using TMPro;
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
    private const float GlowPulseFrequency = 0.4f;

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
        ItemTable itemRow = dtItem.GetDataRow(treasureRow.ItemTableId);

        if (itemRow == null)
        {
            DebugEx.Warning(
                nameof(TreasureItemUI),
                $"未找到ItemTable数据: ItemTableId={treasureRow.ItemTableId}"
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
        ItemTable itemRow = dtItem.GetDataRow(treasureRow.ItemTableId);

        if (itemRow == null)
            return;

        // 加载宝物的图标
        await ResourceExtension.LoadSpriteAsync(itemRow.IconId, varTreasureImg);
    }

    /// <summary>
    /// 根据稀有度应用发光效果（参考InventoryItemUI）
    /// </summary>
    private void ApplyRarityGlow(int rarity)
    {
        if (m_GlowImage == null)
        {
            var glowTransform = transform.Find("GlowEffect");
            if (glowTransform == null)
            {
                DebugEx.Warning(nameof(TreasureItemUI), "未找到GlowEffect子对象");
                return;
            }
            m_GlowImage = glowTransform.GetComponent<Image>();
            if (m_GlowImage == null)
            {
                DebugEx.Warning(nameof(TreasureItemUI), "GlowEffect上没有Image组件");
                return;
            }
        }

        var shader = Shader.Find("UI/RarityGlow");
        if (shader == null)
        {
            DebugEx.Warning(nameof(TreasureItemUI), "未找到UI/RarityGlow Shader");
            return;
        }

        var mat = new Material(shader);
        m_GlowImage.material = mat;

        // 根据稀有度设置颜色和强度
        Color color = rarity switch
        {
            1 => new Color(0.8f, 0.8f, 0.8f, 1f), // 白色
            2 => new Color(0.2f, 0.8f, 0.2f, 1f), // 绿色
            3 => new Color(0.2f, 0.6f, 1f, 1f), // 蓝色
            4 => new Color(0.8f, 0.2f, 1f, 1f), // 紫色
            5 => new Color(1f, 0.8f, 0.2f, 1f), // 金色
            _ => Color.white,
        };
        float intensity = rarity switch
        {
            1 => 0.8f,
            2 => 1.2f,
            3 => 1.5f,
            4 => 1.8f,
            5 => 2.2f,
            _ => 1.0f,
        };

        mat.SetColor("_GlowColor", color);
        mat.SetFloat("_GlowIntensity", intensity);
        mat.SetFloat("_GlowRadius", 2.0f);
        mat.SetFloat("_EdgeSoftness", 0.35f);

        // 停止之前的动画
        m_GlowPulseTween?.Kill();

        // 创建脉冲动画
        float duration = 1f / (GlowPulseFrequency * 2f);
        m_GlowPulseTween = DOTween
            .To(
                () => mat.GetFloat("_GlowIntensity"),
                v => mat.SetFloat("_GlowIntensity", v),
                intensity * 1.4f,
                duration
            )
            .From(intensity * 0.6f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        DebugEx.Log(
            nameof(TreasureItemUI),
            $"应用稀有度发光效果: rarity={rarity}, color={color}, intensity={intensity}"
        );
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
