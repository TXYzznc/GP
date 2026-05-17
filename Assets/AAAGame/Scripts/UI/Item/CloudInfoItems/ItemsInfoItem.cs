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
        DebugEx.Log(
            "ItemsInfoItem",
            $"SetData: iconArr长度={varItemIconArr?.Length ?? -1}, iconIds长度={itemIconIds?.Length ?? -1}, totalNums={totalNums}"
        );

        // 设置物品图标
        if (varItemIconArr != null && itemIconIds != null)
        {
            int count = Mathf.Min(varItemIconArr.Length, itemIconIds.Length);
            for (int i = 0; i < count; i++)
            {
                if (varItemIconArr[i] != null)
                {
                    DebugEx.Log("ItemsInfoItem", $"加载图标[{i}]: id={itemIconIds[i]}");
                    varItemIconArr[i].SetSpriteById(itemIconIds[i]);
                }
                else
                {
                    DebugEx.Warning("ItemsInfoItem", $"varItemIconArr[{i}] 为 null");
                }
            }
        }
        else
        {
            DebugEx.Warning(
                "ItemsInfoItem",
                $"varItemIconArr 或 itemIconIds 为 null，跳过图标加载"
            );
        }

        // 设置物品数量
        if (varCoinNumsArr != null && coinNums != null)
        {
            int count = Mathf.Min(varCoinNumsArr.Length, coinNums.Length);
            for (int i = 0; i < count; i++)
            {
                if (varCoinNumsArr[i] != null)
                    varCoinNumsArr[i].text = coinNums[i].ToString();
            }
        }

        if (varNums != null)
            varNums.text = totalNums.ToString();
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
                if (text != null)
                    text.text = "";
            }
        }
    }
}
