using System;
using Cysharp.Threading.Tasks;
using GameFramework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 出战预设界面 - 棋子项（精简版，仅用于预设界面）
/// 相比 ChessItemUI，移除了拖拽、扇形容器、死亡状态等战斗相关功能
/// </summary>
public partial class ChessPresetItem : UIItemBase
{
    #region 字段

    private int m_ChessId;
    private Action<int> m_OnClickCallback;

    #endregion

    #region 生命周期

    protected override void OnInit()
    {
        base.OnInit();

        if (varBtn != null)
        {
            varBtn.onClick.AddListener(OnButtonClick);
        }
    }

    private void OnDestroy()
    {
        if (varBtn != null)
        {
            varBtn.onClick.RemoveListener(OnButtonClick);
        }
    }

    #endregion

    #region 数据设置

    /// <summary>
    /// 设置棋子数据
    /// </summary>
    public void SetData(int chessId, Action<int> onClickCallback = null)
    {
        m_ChessId = chessId;
        m_OnClickCallback = onClickCallback;

        if (ChessDataManager.Instance.TryGetConfig(chessId, out var config))
        {
            if (varNameText != null)
                varNameText.text = config.Name;

            LoadIconAsync(config.GetIconId(1));
            SetQualityUI(config.Quality);
            LoadSynergyIconsAsync(chessId).Forget();

            DebugEx.Log("ChessPresetItem", $"SetData: chessId={chessId}, name={config.Name}");
        }
        else
        {
            DebugEx.Error(
                "ChessPresetItem",
                $"SetData failed: config not found for chessId={chessId}"
            );
        }
    }

    /// <summary>
    /// 根据稀有度设置卡牌框和背景
    /// </summary>
    private void SetQualityUI(int quality)
    {
        int cardFrameId = 19000 + quality;
        int bgId = 19010 + quality;

        if (varCardFrame != null)
            _ = GameExtension.ResourceExtension.LoadSpriteAsync(cardFrameId, varCardFrame, 1f);

        if (varBg != null)
            _ = GameExtension.ResourceExtension.LoadSpriteAsync(bgId, varBg, 1f);
    }

    private void LoadIconAsync(int iconResourceId)
    {
        if (varImage == null)
            return;
        _ = GameExtension.ResourceExtension.LoadSpriteAsync(iconResourceId, varImage, 1f);
    }

    /// <summary>
    /// 加载羁绊图标
    /// </summary>
    private async UniTaskVoid LoadSynergyIconsAsync(int chessId)
    {
        if (varSynergy == null || varSynergyIcon == null)
            return;

        // 清除旧的克隆图标
        for (int i = varSynergy.transform.childCount - 1; i >= 0; i--)
        {
            var child = varSynergy.transform.GetChild(i);
            if (child.gameObject != varSynergyIcon)
                UnityEngine.Object.Destroy(child.gameObject);
        }

        var chessDt = GF.DataTable.GetDataTable<SummonChessTable>();
        var chessRow = chessDt?.GetDataRow(chessId);
        if (chessRow == null)
            return;

        var synergyDt = GF.DataTable.GetDataTable<SynergyTable>();
        if (synergyDt == null)
            return;

        var synergyIds = new System.Collections.Generic.List<int>();
        if (chessRow.Races != null)
            synergyIds.AddRange(chessRow.Races);
        if (chessRow.Classes != null)
            synergyIds.AddRange(chessRow.Classes);

        bool hasAny = false;
        foreach (int synergyId in synergyIds)
        {
            if (synergyId <= 0)
                continue;
            var synergyRow = synergyDt.GetDataRow(synergyId);
            if (synergyRow == null || synergyRow.IconId <= 0)
                continue;

            var iconGo = UnityEngine.Object.Instantiate(varSynergyIcon, varSynergy.transform);
            iconGo.SetActive(true);

            var iconImage = iconGo.GetComponent<Image>();
            if (iconImage != null)
                await GameExtension.ResourceExtension.LoadSpriteAsync(synergyRow.IconId, iconImage);

            hasAny = true;
        }

        varSynergy.gameObject.SetActive(hasAny);
        DebugEx.Log(
            "ChessPresetItem",
            $"LoadSynergyIconsAsync: chessId={chessId}, hasAny={hasAny}"
        );
    }

    #endregion

    #region 按钮事件

    private void OnButtonClick()
    {
        m_OnClickCallback?.Invoke(m_ChessId);
        DebugEx.Log("ChessPresetItem", $"OnButtonClick: chessId={m_ChessId}");
    }

    #endregion

    #region Mask 管理

    public void HideMask()
    {
        if (varMask != null)
            varMask.SetActive(false);

        if (varMaskText != null)
            varMaskText.gameObject.SetActive(false);
    }

    public void ShowSelectedMask()
    {
        if (varMask != null)
            varMask.SetActive(true);

        if (varMaskText != null)
        {
            varMaskText.text = "已选中";
            varMaskText.gameObject.SetActive(true);
        }
    }

    public void SetEmpty()
    {
        m_ChessId = 0;
        m_OnClickCallback = null;

        if (varNameText != null)
            varNameText.text = string.Empty;
        if (varImage != null)
            varImage.sprite = null;
        if (varCardFrame != null)
            varCardFrame.sprite = null;
        if (varBg != null)
            varBg.sprite = null;
        if (varSynergy != null)
            varSynergy.gameObject.SetActive(false);
        HideMask();

        DebugEx.Log("ChessPresetItem", "SetEmpty: 设置为空占位");
    }

    public string GetChessName()
    {
        if (ChessDataManager.Instance.TryGetConfig(m_ChessId, out var config))
            return config.Name;
        return string.Empty;
    }

    #endregion
}
