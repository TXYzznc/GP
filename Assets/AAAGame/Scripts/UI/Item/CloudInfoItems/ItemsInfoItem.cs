using UnityEngine;
using UnityEngine.UI;

public partial class ItemsInfoItem : UIItemBase
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
    /// 设置物品信息
    /// </summary>
    public void SetData(int[] itemIconIds, int[] coinNums, int totalNums = 999)
    {
        // 设置物品图标
        if (varItemIconArr != null && itemIconIds != null)
        {
            int count = Mathf.Min(varItemIconArr.Length, itemIconIds.Length);
            for (int i = 0; i < count; i++)
            {
                if (varItemIconArr[i] != null)
                {
                    varItemIconArr[i].SetSpriteById(itemIconIds[i]);
                }
            }
        }

        // 设置物品数量
        if (varCoinNumsArr != null && coinNums != null)
        {
            int count = Mathf.Min(varCoinNumsArr.Length, coinNums.Length);
            for (int i = 0; i < count; i++)
            {
                if (varCoinNumsArr[i] != null)
                {
                    varCoinNumsArr[i].text = coinNums[i].ToString();
                }
            }
        }
        if (varNums != null)
        {
            varNums.text = "totalNums";
        }
    }

    /// <summary>
    /// 清空数据
    /// </summary>
    public void ClearData()
    {
        if (varCoinNumsArr != null)
        {
            foreach (var text in varCoinNumsArr)
            {
                if (text != null) text.text = "";
            }
        }
    }
}
