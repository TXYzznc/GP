using UnityEngine;
using UnityEngine.UI;

public partial class TimeInfoItem : UIItemBase
{
    private void Start()
    {
        // 确保 RectTransform 正确设置
        var rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }
    }
    /// <summary>
    /// 设置时间信息
    /// </summary>
    public void SetData(string chapterTitle, int days, string time)
    {
        if (varChapterTitle != null)
            varChapterTitle.text = chapterTitle;

        if (varDays != null)
            varDays.text = $"Day {days}";

        if (varTime != null)
            varTime.text = time;
    }

    /// <summary>
    /// 清理数据
    /// </summary>
    public void ClearData()
    {
        if (varChapterTitle != null) varChapterTitle.text = "";
        if (varDays != null) varDays.text = "";
        if (varTime != null) varTime.text = "";
    }
}
