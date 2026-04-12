using System;
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
            // 设置名称
            if (varNameText != null)
            {
                varNameText.text = config.Name;
            }

            // 设置星级
            if (varStar != null)
            {
                varStar.text = new string('★', config.StarLevel);
            }

            // 加载图标
            LoadIconAsync(config.IconId);

            // 设置稀有度UI
            SetQualityUI(config.Quality);

            DebugEx.Log(
                "ChessPresetItem",
                $"SetData: chessId={chessId}, name={config.Name}, star={config.StarLevel}"
            );
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
    /// 根据稀有度设置卡牌框、背景和名字背景
    /// </summary>
    private void SetQualityUI(int quality)
    {
        int cardFrameId = 19000 + quality;
        int bgId = 19010 + quality;
        int maskId = 19020 + quality;

        if (varCardFrame != null)
        {
            _ = GameExtension.ResourceExtension.LoadSpriteAsync(cardFrameId, varCardFrame, 1f);
        }

        if (varBg != null)
        {
            _ = GameExtension.ResourceExtension.LoadSpriteAsync(bgId, varBg, 1f);
        }

        if (varNameBg != null)
        {
            var maskImage = varNameBg.GetComponent<Image>();
            if (maskImage != null)
            {
                _ = GameExtension.ResourceExtension.LoadSpriteAsync(maskId, maskImage, 0.8f);
            }
        }
    }

    /// <summary>
    /// 异步加载图标
    /// </summary>
    private void LoadIconAsync(int iconResourceId)
    {
        if (varImage == null)
            return;

        _ = GameExtension.ResourceExtension.LoadSpriteAsync(iconResourceId, varImage, 1f);
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

    /// <summary>
    /// 隐藏 Mask（默认状态）
    /// </summary>
    public void HideMask()
    {
        if (varMask != null)
        {
            varMask.SetActive(false);
        }

        if (varMaskText != null)
        {
            varMaskText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 显示 Mask 并设置"已选中"文本
    /// </summary>
    public void ShowSelectedMask()
    {
        if (varMask != null)
        {
            varMask.SetActive(true);
        }

        if (varMaskText != null)
        {
            varMaskText.text = "已选中";
            varMaskText.gameObject.SetActive(true);
        }
    }

    #endregion
}
