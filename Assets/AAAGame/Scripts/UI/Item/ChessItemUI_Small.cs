using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using GameExtension;
using Cysharp.Threading.Tasks;

/// <summary>
/// 棋子小卡片 - 在 CharacterBagUI 的列表中显示
/// </summary>
public partial class ChessItemUI_Small : UIItemBase, IPointerClickHandler
{
    public delegate void OnChessSelectedDelegate(int chessId);
    public event OnChessSelectedDelegate OnChessSelected;

    private int m_ChessId;
    private SummonChessConfig m_ChessConfig;

    private void OnEnable()
    {
        if (varChessImg == null)
        {
            DebugEx.Error(nameof(ChessItemUI_Small), "ChessItemUI_Small 缺少 UI 元素引用");
            return;
        }
    }

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

        // 显示棋子名称
        if (varNameText != null)
        {
            varNameText.text = config.Name;
        }

        // 显示品质指示器
        UpdateQualityDisplay(config.Quality);

        // 异步加载棋子图标
        LoadChessImageAsync(config).Forget();
    }

    private async UniTask LoadChessImageAsync(SummonChessConfig config)
    {
        if (varChessImg == null || config == null)
            return;

        int iconId = config.GetIconId(1);
        await ResourceExtension.LoadSpriteAsync(iconId, varChessImg);
    }

    private void UpdateQualityDisplay(int quality)
    {
        if (varQualityPanel == null || varQualityItem1Arr == null || varQualityItem1Arr.Length == 0)
            return;

        // 根据品质显示对应数量的品质元素
        for (int i = 0; i < varQualityItem1Arr.Length; i++)
        {
            if (varQualityItem1Arr[i] != null)
            {
                varQualityItem1Arr[i].gameObject.SetActive(i < quality);
            }
        }
    }

    public void OnCardSelected()
    {
        OnChessSelected?.Invoke(m_ChessId);
    }

    public void SetHighlight(bool isHighlighted)
    {
        // 由 CharacterBagUI 控制高亮状态
        // 这里可以添加高亮效果的逻辑
    }

    public int GetChessId() => m_ChessId;
    public SummonChessConfig GetChessConfig() => m_ChessConfig;
}
