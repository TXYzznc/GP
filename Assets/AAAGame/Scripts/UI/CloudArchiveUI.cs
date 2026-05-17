using System;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

/// <summary>
/// 云存档UI界面
/// </summary>
public partial class CloudArchiveUI : UIFormBase
{
    #region 生命周期

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        DebugEx.Log(nameof(CloudArchiveUI), "已打开");
        RefreshArchiveInfo();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
        DebugEx.Log(nameof(CloudArchiveUI), "已关闭");
    }

    #endregion

    #region 按钮事件

    protected override void OnButtonClick(object sender, Button btSelf)
    {
        base.OnButtonClick(sender, btSelf);

        if (btSelf == varUpLoadBtn)
            OnUploadButtonClick();
        else if (btSelf == varDownLoadBtn)
            OnDownloadButtonClick();
        else if (btSelf == varCloseBtn)
            OnCloseButtonClick();
    }

    private void OnUploadButtonClick()
    {
        DebugEx.Log(nameof(CloudArchiveUI), "上传存档");
        GF.UI.ShowToast("正在上传存档到云端...", UIExtension.ToastStyle.Blue);
    }

    private void OnDownloadButtonClick()
    {
        DebugEx.Log(nameof(CloudArchiveUI), "下载存档");
        GF.UI.ShowToast("正在从云端下载存档...", UIExtension.ToastStyle.Blue);
    }

    private void OnCloseButtonClick()
    {
        DebugEx.Log(nameof(CloudArchiveUI), "关闭界面");
        GF.UI.Close(this.UIForm);
    }

    #endregion

    #region 刷新存档信息

    private void RefreshArchiveInfo()
    {
        RefreshLocalArchive();
        RefreshCloudArchive();
    }

    /// <summary>
    /// 刷新本地存档（使用当前已加载的存档数据，不触发运行时初始化）
    /// </summary>
    private void RefreshLocalArchive()
    {
        // 优先用已加载的存档，否则只读加载第一个存档
        var saveData = PlayerAccountDataManager.Instance.CurrentSaveData;
        if (saveData == null)
        {
            var saves = PlayerAccountDataManager.Instance.GetAllSaveBriefInfos();
            if (saves == null || saves.Count == 0)
            {
                DebugEx.Warning(nameof(CloudArchiveUI), "本地无存档数据");
                return;
            }
            saveData = PlayerAccountDataManager.Instance.ReadSaveDataReadOnly(saves[0].SaveId);
            if (saveData == null)
            {
                DebugEx.Warning(nameof(CloudArchiveUI), "只读加载存档失败");
                return;
            }
        }

        // 召唤师名称
        var summonerTable = GF.DataTable.GetDataTable<SummonerTable>();
        var summonerRow = summonerTable?.GetDataRow(saveData.CurrentSummonerId);
        string summonerName = summonerRow != null ? summonerRow.Name : saveData.SaveName;

        // 仓库道具总数
        int totalItems = 0;
        var inventoryItems = saveData.GetInventoryItems();
        foreach (var item in inventoryItems)
            totalItems += item.Count;

        // 货币（金币/灵石/起源石）
        int[] iconIds = new int[]
        {
            ResourceIds.ICON_GOLD,
            ResourceIds.ICON_MAGICAL_STONE,
            ResourceIds.ICON_HOLY_WATER,
        };
        int[] nums = new int[] { saveData.Gold, saveData.SpiritStone, saveData.OriginStone };

        string lastPlayTime = TimestampToString(saveData.LastPlayTime);

        varPlayerInfo_Left?.SetData(summonerName, summonerName, saveData.GlobalLevel);
        varItemsInfo_Left?.SetData(iconIds, nums, totalItems);
        varTimeInfo_Left?.SetData("第一章：序幕", 2, lastPlayTime);

        DebugEx.Log(
            nameof(CloudArchiveUI),
            $"本地存档已刷新: {summonerName} Lv.{saveData.GlobalLevel}"
        );
    }

    /// <summary>
    /// 刷新云端存档（模拟数据）
    /// </summary>
    private void RefreshCloudArchive()
    {
        int[] iconIds = new int[]
        {
            ResourceIds.ICON_GOLD,
            ResourceIds.ICON_MAGICAL_STONE,
            ResourceIds.ICON_HOLY_WATER,
        };
        int[] nums = new int[] { 12800, 350, 88 };

        varPlayerInfo_Right?.SetData("狂战士", "战士", 12);
        varItemsInfo_Right?.SetData(iconIds, nums, 142);
        varTimeInfo_Right?.SetData("第一章：序幕", 1, "2025/11/28 10:15:30");

        DebugEx.Log(nameof(CloudArchiveUI), "云端存档（模拟数据）已刷新");
    }

    #endregion

    #region 辅助方法

    private string TimestampToString(double timestamp)
    {
        if (timestamp <= 0)
            return "暂无记录";
        try
        {
            var dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(timestamp)
                .ToLocalTime();
            return dt.ToString("yyyy/MM/dd HH:mm:ss");
        }
        catch
        {
            return "暂无记录";
        }
    }

    #endregion
}
