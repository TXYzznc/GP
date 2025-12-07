using UnityEngine;
using UnityEngine.UI;

public partial class PlayerInfoItem : UIItemBase
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
    /// 设置玩家信息
    /// </summary>
    public void SetData(string playerName, string occupation, int grade)
    {
        if (varPlayerName != null)
            varPlayerName.text = playerName;
        
        if (varOccupation != null)
            varOccupation.text = occupation;
        
        if (varGrade != null)
            varGrade.text = $"等级: {grade}";
        
        // 如果需要设置头像
        // if (varImage != null)
        //     varImage.SetSpriteById(avatarId);
    }

    /// <summary>
    /// 清理数据
    /// </summary>
    public void ClearData()
    {
        if (varPlayerName != null) varPlayerName.text = "";
        if (varOccupation != null) varOccupation.text = "";
        if (varGrade != null) varGrade.text = "";
    }
}
