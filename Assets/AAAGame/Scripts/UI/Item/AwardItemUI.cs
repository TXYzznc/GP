using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GameExtension;
using Cysharp.Threading.Tasks;
using UnityGameFramework.Runtime;
using DG.Tweening;

public partial class AwardItemUI : UIItemBase, IPointerEnterHandler, IPointerExitHandler
{
    private ItemTable m_Row;
    private Tween m_ClickScaleTween;
    private RectTransform m_RectTransform;
    private Image m_GlowImage;
    private Tween m_GlowPulseTween;

    protected override void OnInit()
    {
        base.OnInit();
        m_RectTransform = GetComponent<RectTransform>();

        if (varBtn != null)
        {
            varBtn.onClick.AddListener(OnClickAward);
        }

        if (varFrame != null)
        {
            varFrame.gameObject.SetActive(false);
        }
    }

    public void SetData(int itemId)
    {
        var table = GF.DataTable.GetDataTable<ItemTable>();
        if (table != null && table.HasDataRow(itemId))
        {
            m_Row = table.GetDataRow(itemId);
        }
        else
        {
            m_Row = null;
        }

        if (varAwardName != null)
        {
            varAwardName.text = m_Row?.Name ?? string.Empty;
        }

        ApplyRarityGlow();

        int iconId = m_Row != null ? m_Row.IconId : 0;
        DebugEx.Log("AwardItemUI", $"SetData itemId={itemId} iconId={iconId} t={Time.time:F3} f={Time.frameCount}");
        LoadIconAsync(iconId).Forget();
    }

    private void ApplyRarityGlow()
    {
        if (m_Row == null)
        {
            return;
        }

        // 查找发光对象
        if (m_GlowImage == null)
        {
            var glowTransform = transform.Find("GlowEffect");
            if (glowTransform == null)
            {
                DebugEx.Warning("AwardItemUI", "找不到 GlowEffect 对象");
                return;
            }
            m_GlowImage = glowTransform.GetComponent<Image>();
            if (m_GlowImage == null)
            {
                DebugEx.Warning("AwardItemUI", "GlowEffect 对象没有 Image 组件");
                return;
            }
        }

        // 为每个物品创建独立的材质（代码中自动创建，预制体中不需要配置）
        var shader = Shader.Find("UI/RarityGlow");
        if (shader == null)
        {
            DebugEx.Error("AwardItemUI", "找不到 UI/RarityGlow 着色器");
            return;
        }

        var uniqueMaterial = new Material(shader);
        m_GlowImage.material = uniqueMaterial;

        // 根据稀有度设置发光参数
        Color glowColor = GetGlowColorByRarity(m_Row.Rarity);
        float baseIntensity = GetGlowIntensityByRarity(m_Row.Rarity);

        uniqueMaterial.SetColor("_GlowColor", glowColor);
        uniqueMaterial.SetFloat("_GlowIntensity", baseIntensity);
        uniqueMaterial.SetFloat("_GlowRadius", 2.0f);
        uniqueMaterial.SetFloat("_EdgeSoftness", 0.35f);

        ApplyGlowPulseAnimation(uniqueMaterial, baseIntensity);

        DebugEx.Log("AwardItemUI", $"✓ 稀有度发光已应用: Rarity={m_Row.Rarity}, Color={glowColor}, Intensity={baseIntensity}");
    }

    private const float GlowPulseFrequency = 0.4f; // 每秒脉冲次数

    private void ApplyGlowPulseAnimation(Material material, float baseIntensity)
    {
        m_GlowPulseTween?.Kill();

        float duration = 1f / (GlowPulseFrequency * 2f);

        m_GlowPulseTween = DOTween.To(
            () => material.GetFloat("_GlowIntensity"),
            (value) => material.SetFloat("_GlowIntensity", value),
            baseIntensity * 1.4f,
            duration
        )
        .From(baseIntensity * 0.6f)
        .SetLoops(-1, LoopType.Yoyo)
        .SetEase(Ease.InOutSine);
    }

    private Color GetGlowColorByRarity(int rarity)
    {
        return rarity switch
        {
            1 => new Color(0.8f, 0.8f, 0.8f, 1f),  // 白色：普通
            2 => new Color(0.2f, 0.8f, 0.2f, 1f),  // 绿色：稀有
            3 => new Color(0.2f, 0.6f, 1f, 1f),    // 蓝色：史诗
            4 => new Color(0.8f, 0.2f, 1f, 1f),    // 紫色：传奇
            5 => new Color(1f, 0.8f, 0.2f, 1f),    // 金色：神话
            _ => Color.white
        };
    }

    private float GetGlowIntensityByRarity(int rarity)
    {
        return rarity switch
        {
            1 => 0.8f,   // 普通：较弱
            2 => 1.2f,   // 稀有
            3 => 1.5f,   // 史诗
            4 => 1.8f,   // 传奇
            5 => 2.2f,   // 神话：最强
            _ => 1.0f
        };
    }

    private async UniTaskVoid LoadIconAsync(int iconId)
    {
        if (varAwardImg == null)
        {
            return;
        }

        if (iconId <= 0)
        {
            varAwardImg.sprite = null;
            varAwardImg.color = new Color(1f, 1f, 1f, 0f);
            DebugEx.Log("AwardItemUI", $"LoadIcon skip iconId={iconId} t={Time.time:F3} f={Time.frameCount}");
            return;
        }

        try
        {
            float startTime = Time.time;
            int startFrame = Time.frameCount;
            DebugEx.Log("AwardItemUI", $"LoadIcon start iconId={iconId} t={startTime:F3} f={startFrame}");

            if (varAwardImg != null)
            {
                await ResourceExtension.LoadSpriteAsync(iconId, varAwardImg, 1f, null);
                varAwardImg.color = Color.white;
                DebugEx.Log("AwardItemUI", $"LoadIcon done iconId={iconId} t={Time.time:F3} f={Time.frameCount} dt={(Time.time - startTime):F3} df={(Time.frameCount - startFrame)}");
            }
            else
            {
                DebugEx.Warning("AwardItemUI", $"LoadIcon failed: Image为null, iconId={iconId} t={Time.time:F3} f={Time.frameCount}");
            }
        }
        catch (Exception)
        {
            if (varAwardImg != null)
            {
                varAwardImg.sprite = null;
                varAwardImg.color = new Color(1f, 1f, 1f, 0f);
            }
            DebugEx.Warning("AwardItemUI", $"LoadIcon exception iconId={iconId} t={Time.time:F3} f={Time.frameCount}");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (varFrame != null && !varFrame.gameObject.activeSelf)
        {
            varFrame.gameObject.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (varFrame != null && varFrame.gameObject.activeSelf)
        {
            varFrame.gameObject.SetActive(false);
        }
    }

    private void OnClickAward()
    {
        if (m_Row == null)
        {
            return;
        }

        PlayClickAnimation();
        ShowItemDetailAsync().Forget();
    }

    private void PlayClickAnimation()
    {
        if (m_RectTransform == null)
        {
            return;
        }

        m_ClickScaleTween?.Kill();
        Vector3 originalScale = m_RectTransform.localScale;

        m_ClickScaleTween = m_RectTransform
            .DOScale(originalScale * 0.95f, 0.1f)
            .OnComplete(() =>
            {
                m_ClickScaleTween = m_RectTransform
                    .DOScale(originalScale, 0.15f)
                    .SetEase(Ease.OutBack);
            });
    }

    private async UniTaskVoid ShowItemDetailAsync()
    {
        await UniTask.Delay(50);

        var detailText = BuildDetailText();
        if (m_RectTransform != null)
        {
            GF.UI.ShowFloatingTipAt(detailText, m_RectTransform, new Vector2(10f, 0f));
        }
    }

    private string BuildDetailText()
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"<b>{m_Row.Name}</b>");
        sb.AppendLine();

        if (m_Row.Rarity > 0)
        {
            sb.AppendLine($"品质: {m_Row.Rarity}");
        }

        if (m_Row.Weight > 0)
        {
            sb.AppendLine($"重量: {m_Row.Weight}g");
        }

        // 从专门数据获取物品详情
        int itemType = m_Row.Type;
        if (itemType == (int)ItemType.Treasure)
        {
            var treasureData = ItemManager.Instance?.GetTreasureData(m_Row.Id);
            if (treasureData != null)
            {
                if (treasureData.SynergyIds != null && treasureData.SynergyIds.Count > 0)
                    sb.AppendLine($"羁绊: {string.Join(", ", treasureData.SynergyIds)}");

                if (treasureData.BaseAttributes != null && treasureData.BaseAttributes.Count > 0)
                {
                    sb.AppendLine("[基础属性]");
                    foreach (var attr in treasureData.BaseAttributes)
                        sb.AppendLine($"  {attr.Key}: +{attr.Value}");
                }

                if (treasureData.SpecialEffectId > 0)
                    sb.AppendLine($"特殊效果: ID={treasureData.SpecialEffectId}");
            }
        }
        else if (itemType == (int)ItemType.Equipment)
        {
            var equipData = ItemManager.Instance?.GetEquipmentData(m_Row.Id);
            if (equipData != null)
            {
                if (equipData.BaseAttributes != null && equipData.BaseAttributes.Count > 0)
                {
                    sb.AppendLine("[基础属性]");
                    foreach (var attr in equipData.BaseAttributes)
                        sb.AppendLine($"  {attr.Key}: +{attr.Value}");
                }

                if (equipData.SpecialEffectId > 0)
                    sb.AppendLine($"特殊效果: ID={equipData.SpecialEffectId}");
            }
        }

        if (!string.IsNullOrEmpty(m_Row.Description))
        {
            sb.AppendLine();
            sb.AppendLine($"<color=#808080>{m_Row.Description}</color>");
        }

        return sb.ToString();
    }
}
