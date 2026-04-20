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

        SetQualityColor();

        int iconId = m_Row != null ? m_Row.IconId : 0;
        DebugEx.LogModule("AwardItemUI", $"SetData itemId={itemId} iconId={iconId} t={Time.time:F3} f={Time.frameCount}");
        LoadIconAsync(iconId).Forget();
    }

    private void SetQualityColor()
    {
        if (varBg == null || m_Row == null)
        {
            return;
        }

        Color qualityColor = GetColorByQuality(m_Row.Quality);
        varBg.color = qualityColor;
    }

    private Color GetColorByQuality(int quality)
    {
        return quality switch
        {
            1 => new Color(0.8f, 0.8f, 0.8f, 1f),  // 白色：普通
            2 => new Color(0.2f, 0.8f, 0.2f, 1f),  // 绿色：稀有
            3 => new Color(0.2f, 0.6f, 1f, 1f),    // 蓝色：史诗
            4 => new Color(0.8f, 0.2f, 1f, 1f),    // 紫色：传奇
            5 => new Color(1f, 0.8f, 0.2f, 1f),    // 金色：神话
            _ => Color.white
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
            DebugEx.LogModule("AwardItemUI", $"LoadIcon skip iconId={iconId} t={Time.time:F3} f={Time.frameCount}");
            return;
        }

        try
        {
            float startTime = Time.time;
            int startFrame = Time.frameCount;
            DebugEx.LogModule("AwardItemUI", $"LoadIcon start iconId={iconId} t={startTime:F3} f={startFrame}");

            if (varAwardImg != null)
            {
                await ResourceExtension.LoadSpriteAsync(iconId, varAwardImg, 1f, null);
                varAwardImg.color = Color.white;
                DebugEx.LogModule("AwardItemUI", $"LoadIcon done iconId={iconId} t={Time.time:F3} f={Time.frameCount} dt={(Time.time - startTime):F3} df={(Time.frameCount - startFrame)}");
            }
            else
            {
                DebugEx.WarningModule("AwardItemUI", $"LoadIcon failed: Image为null, iconId={iconId} t={Time.time:F3} f={Time.frameCount}");
            }
        }
        catch (Exception)
        {
            if (varAwardImg != null)
            {
                varAwardImg.sprite = null;
                varAwardImg.color = new Color(1f, 1f, 1f, 0f);
            }
            DebugEx.WarningModule("AwardItemUI", $"LoadIcon exception iconId={iconId} t={Time.time:F3} f={Time.frameCount}");
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

        if (m_Row.Quality > 0)
        {
            sb.AppendLine($"品质: {m_Row.Quality}");
        }

        if (m_Row.Weight > 0)
        {
            sb.AppendLine($"重量: {m_Row.Weight}g");
        }

        if (m_Row.AffixPoolIds != null && m_Row.AffixPoolIds.Length > 0)
        {
            string affixes = string.Join(", ", m_Row.AffixPoolIds);
            sb.AppendLine($"词条: {affixes}");
        }

        if (m_Row.SynergyIds != null && m_Row.SynergyIds.Length > 0)
        {
            string synergies = string.Join(", ", m_Row.SynergyIds);
            sb.AppendLine($"羁绊: {synergies}");
        }

        if (!string.IsNullOrEmpty(m_Row.BaseAttributes))
        {
            sb.AppendLine($"基础属性: {m_Row.BaseAttributes}");
        }

        if (m_Row.SpecialEffectId > 0)
        {
            sb.AppendLine($"特殊效果: ID={m_Row.SpecialEffectId}");
        }

        if (!string.IsNullOrEmpty(m_Row.Description))
        {
            sb.AppendLine();
            sb.AppendLine($"<color=#808080>{m_Row.Description}</color>");
        }

        return sb.ToString();
    }
}
