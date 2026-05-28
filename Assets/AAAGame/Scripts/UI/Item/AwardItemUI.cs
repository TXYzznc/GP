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
            return;

        m_GlowPulseTween?.Kill();
        m_GlowPulseTween = RarityGlowHelper.Apply(transform, m_Row.Rarity, ref m_GlowImage);
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

                string statusText = ItemDetailHelper.BuildStatusText(m_Row.Id, ItemType.Treasure);
                if (!string.IsNullOrEmpty(statusText))
                    sb.AppendLine(statusText);
            }
        }
        else if (itemType == (int)ItemType.Equipment)
        {
            var equipData = ItemManager.Instance?.GetEquipmentData(m_Row.Id);
            if (equipData != null)
            {
                string statusText = ItemDetailHelper.BuildStatusText(m_Row.Id, ItemType.Equipment);
                if (!string.IsNullOrEmpty(statusText))
                    sb.AppendLine(statusText);
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
