using Cysharp.Threading.Tasks;
using GameExtension;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 棋子小卡片 - 在 CharacterBagUI 的列表中显示
/// </summary>
public partial class ChessItemUI_Small : UIItemBase, IPointerClickHandler
{
    public delegate void OnChessSelectedDelegate(int chessId);
    public event OnChessSelectedDelegate OnChessSelected;

    private int m_ChessId;
    private SummonChessConfig m_ChessConfig;

    public void OnPointerClick(PointerEventData eventData)
    {
        OnCardSelected();
    }

    public void InitChess(int chessId, SummonChessConfig config)
    {
        if (config == null)
        {
            DebugEx.Error(nameof(ChessItemUI_Small), $"棋子 {chessId} 的配置为空");
            return;
        }

        m_ChessId = chessId;
        m_ChessConfig = config;

        if (varNameText != null)
            varNameText.text = config.Name;

        // 默认隐藏高亮
        if (varHighlightImage != null)
            varHighlightImage.gameObject.SetActive(false);

        LoadChessImageAsync(config).Forget();
        LoadQualityUIAsync(config.Quality).Forget();
    }

    private async UniTask LoadChessImageAsync(SummonChessConfig config)
    {
        if (varChessImg == null || config == null)
            return;
        await ResourceExtension.LoadSpriteAsync(config.GetIconId(1), varChessImg);
    }

    /// <summary>
    /// 根据稀有度加载 Frame、Bg 和 Decorate，资源ID规则与 ChessItemUI 一致
    /// </summary>
    private async UniTask LoadQualityUIAsync(int quality)
    {
        int frameId = 19000 + quality; // 19001~19004
        int bgId = 19010 + quality; // 19011~19014
        int decorateId = 19020 + quality; // 19021~19024

        if (varFrame != null)
            await ResourceExtension.LoadSpriteAsync(frameId, varFrame);

        if (varBg != null)
            await ResourceExtension.LoadSpriteAsync(bgId, varBg);

        if (varDecorate != null)
            await ResourceExtension.LoadSpriteAsync(decorateId, varDecorate);

        DebugEx.Log(
            nameof(ChessItemUI_Small),
            $"加载稀有度UI: quality={quality}, frameId={frameId}, bgId={bgId}, decorateId={decorateId}"
        );
    }

    public void OnCardSelected()
    {
        OnChessSelected?.Invoke(m_ChessId);
    }

    public void SetHighlight(bool isHighlighted)
    {
        if (varHighlightImage != null)
            varHighlightImage.gameObject.SetActive(isHighlighted);
    }

    public int GetChessId() => m_ChessId;

    public SummonChessConfig GetChessConfig() => m_ChessConfig;
}
