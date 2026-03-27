using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityGameFramework.Runtime;
using GameExtension;

public partial class BuffItem : UIItemBase, IPointerEnterHandler, IPointerExitHandler
{
    private int m_BuffId = 0;
    private int m_StackCount = 1;
    private BuffTable m_BuffConfig = null;
    private int m_FloatingTipId = -1;  // 悬浮提示框ID

    #region 数据设置

    /// <summary>
    /// 设置Buff数据
    /// </summary>
    /// <param name="buffId">Buff ID</param>
    public void SetData(int buffId)
    {
        m_BuffId = buffId;
        m_StackCount = 1;

        // 获取Buff配置
        var buffTable = GF.DataTable.GetDataTable<BuffTable>();
        m_BuffConfig = buffTable?.GetDataRow(buffId);

        if (m_BuffConfig == null)
        {
            Log.Warning($"BuffItem: 无法找到 ID 为 {buffId} 的 Buff 配置");
            return;
        }

        RefreshUI();
    }

    /// <summary>
    /// 设置Buff层数
    /// </summary>
    /// <param name="stackCount">堆叠层数</param>
    public void SetStackCount(int stackCount)
    {
        m_StackCount = Mathf.Max(1, stackCount);
        UpdateStackUI();
    }

    #endregion

    #region UI 刷新

    /// <summary>
    /// 刷新UI
    /// </summary>
    private void RefreshUI()
    {
        if (m_BuffConfig == null)
        {
            return;
        }

        // 刷新图片
        UpdateBuffIcon();

        // 刷新层数
        UpdateStackUI();

        // 刷新名称（默认隐藏）
        UpdateBuffName();

        // 绑定按钮事件和悬浮提示
        if (varBtn != null)
        {
            varBtn.onClick.RemoveAllListeners();
            varBtn.onClick.AddListener(OnBuffClicked);
        }
    }

    /// <summary>
    /// 更新Buff图标
    /// </summary>
    private void UpdateBuffIcon()
    {
        if (varBuffImg == null) return;

        if (m_BuffConfig == null)
        {
            varBuffImg.sprite = null;
            return;
        }

        // 从资源管理器异步加载图标到Image对象
        int spriteId = m_BuffConfig.SpriteId;
        if (varBuffImg != null)
        {
            ResourceExtension.LoadSpriteAsync(spriteId, varBuffImg);
        }
    }

    /// <summary>
    /// 更新堆叠层数显示
    /// </summary>
    private void UpdateStackUI()
    {
        if (varBuffNum == null) return;

        varBuffNum.text = $"x{m_StackCount}";
    }

    /// <summary>
    /// 更新Buff名称显示（默认隐藏）
    /// </summary>
    private void UpdateBuffName()
    {
        if (varBuffName == null) return;

        varBuffName.text = m_BuffConfig?.Name ?? "Unknown";
        // 默认隐藏
        if (varBuffName.gameObject != null)
        {
            varBuffName.gameObject.SetActive(false);
        }
    }

    #endregion

    #region 按钮回调

    /// <summary>
    /// Buff点击回调
    /// </summary>
    private void OnBuffClicked()
    {
        if (m_BuffConfig == null) return;

        DebugEx.LogModule("BuffItem", $"点击了Buff - {m_BuffConfig.Name} (ID:{m_BuffId})");
        ShowBuffTip();
    }

    /// <summary>
    /// 鼠标进入时显示悬浮提示
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowBuffTip();
    }

    /// <summary>
    /// 鼠标离开时隐藏悬浮提示
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        HideBuffTip();
    }

    /// <summary>
    /// 显示Buff悬浮提示
    /// </summary>
    private void ShowBuffTip()
    {
        if (m_BuffConfig == null) return;

        HideBuffTip();  // 先关闭之前的提示

        // 显示Buff名称和描述
        string tipText = $"{m_BuffConfig.Name}\n{m_BuffConfig.Desc}";

        if (varBtn != null)
        {
            m_FloatingTipId = GF.UI.ShowFloatingTipAt(tipText, varBtn.GetComponent<RectTransform>(), new Vector2(10f, 10f));
            DebugEx.LogModule("BuffItem", $"显示Buff提示: {m_BuffConfig.Name}, TipId={m_FloatingTipId}");
        }
    }

    /// <summary>
    /// 隐藏Buff悬浮提示
    /// </summary>
    private void HideBuffTip()
    {
        if (m_FloatingTipId >= 0)
        {
            GF.UI.CloseUIForm(m_FloatingTipId);
            m_FloatingTipId = -1;
        }
    }

    #endregion
}
