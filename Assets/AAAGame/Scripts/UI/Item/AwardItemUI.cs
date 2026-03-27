using System;
using UnityEngine;
using UnityEngine.UI;
using GameExtension;
using Cysharp.Threading.Tasks;
using UnityGameFramework.Runtime;

public partial class AwardItemUI : UIItemBase
{
    private int m_ItemId;
    private ItemTable m_Row;

    protected override void OnInit()
    {
        base.OnInit();

        if (varBtn != null)
        {
            varBtn.onClick.AddListener(OnClickAward);
        }
    }

    public void SetData(int itemId)
    {
        m_ItemId = itemId;

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

        int iconId = m_Row != null ? m_Row.IconId : 0;
        DebugEx.LogModule("AwardItemUI", $"SetData itemId={itemId} iconId={iconId} t={Time.time:F3} f={Time.frameCount}");
        LoadIconAsync(iconId).Forget();
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

    private void OnClickAward()
    {
        if (m_Row == null)
        {
            return;
        }

        RectTransform targetRect = null;
        if (varBtn != null)
        {
            targetRect = varBtn.GetComponent<RectTransform>();
        }

        if (targetRect == null)
        {
            targetRect = GetComponent<RectTransform>();
        }

        string text = $"<b>{m_Row.Name}</b>\n{m_Row.Description}";
        GF.UI.ShowFloatingTipAt(text, targetRect, new Vector2(10f, 0f));
    }
}
