using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using GameExtension;
using Cysharp.Threading.Tasks;

/// <summary>
/// 云存档UI界面
/// </summary>
public partial class CloudArchiveUI : UIFormBase
{
    #region 生命周期

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        // 设置背景和标题图片
        // varBG.SetSpriteById(ResourceIds.CLOUD_ARCHIVE_BG);
        // varLocalTitle.SetSpriteById(ResourceIds.LOCAL_TITLE);
        // varCloudTitle.SetSpriteById(ResourceIds.CLOUD_TITLE);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        Log.Info("CloudArchiveUI 已打开");

        // 刷新存档信息显示
        RefreshArchiveInfo();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);

        Log.Info("CloudArchiveUI 已关闭");
    }

    #endregion

    #region 按钮点击事件处理

    protected override void OnButtonClick(object sender, Button btSelf)
    {
        base.OnButtonClick(sender, btSelf);

        if (btSelf == varUpLoadBtn)
        {
            OnUploadButtonClick();
        }
        else if (btSelf == varDownLoadBtn)
        {
            OnDownloadButtonClick();
        }
        else if (btSelf == varCloseBtn)
        {
            OnCloseButtonClick();
        }
    }

    #endregion

    #region 各个按钮逻辑

    private void OnUploadButtonClick()
    {
        Log.Info("上传存档");
        GF.UI.ShowToast("正在上传存档到云端...", UIExtension.ToastStyle.Blue);
    }

    private void OnDownloadButtonClick()
    {
        Log.Info("下载存档");
        GF.UI.ShowToast("正在从云端下载存档...", UIExtension.ToastStyle.Blue);
    }

    private void OnCloseButtonClick()
    {
        Log.Info("关闭云存档界面");
        GF.UI.Close(this.UIForm);
    }

    #endregion

    #region 动态加载和创建 UI Item

    /// <summary>
    /// 刷新存档信息显示
    /// </summary>
    private void RefreshArchiveInfo()
    {
        // 检查数组是否有效
        if (varLeft1Arr == null || varLeft1Arr.Length < 3)
        {
            Log.Error("varLeft1Arr 数组无效，无法创建本地存档 Item");
            return;
        }

        if (varLeft2Arr == null || varLeft2Arr.Length < 3)
        {
            Log.Error("varLeft2Arr 数组无效，无法创建云端存档 Item");
            return;
        }

        // 加载并创建本地存档 Item
        LoadAndCreateLocalItems();

        // 加载并创建云端存档 Item
        LoadAndCreateCloudItems();
    }

    /// <summary>
    /// 加载并创建本地存档 Item（UniTask 版本）
    /// </summary>
    private async void LoadAndCreateLocalItems()
    {
        try
        {
            // 并行加载三个预制体
            var playerPrefabTask = ResourceExtension.LoadPrefabAsync(ResourceIds.PREFAB_PLAYER_INFO_ITEM);
            var itemsPrefabTask = ResourceExtension.LoadPrefabAsync(ResourceIds.PREFAB_ITEMS_INFO_ITEM);
            var timePrefabTask = ResourceExtension.LoadPrefabAsync(ResourceIds.PREFAB_TIME_INFO_ITEM);

            // 等待所有预制体加载完成
            var (playerPrefab, itemsPrefab, timePrefab) = await UniTask.WhenAll(playerPrefabTask, itemsPrefabTask, timePrefabTask);

            // 创建 PlayerInfoItem
            if (playerPrefab != null)
            {
                var item = SpawnItem<UIItemObject>(playerPrefab, varLeft1Arr[0]);
                SetupItemTransform(item.gameObject, varLeft1Arr[0]); // ? 设置父物体和变换
                (item.itemLogic as PlayerInfoItem)?.SetData("本地玩家", "战士", 25);
            }

            // 创建 ItemsInfoItem
            if (itemsPrefab != null)
            {
                var item = SpawnItem<UIItemObject>(itemsPrefab, varLeft1Arr[1]);
                SetupItemTransform(item.gameObject, varLeft1Arr[1]); // ? 设置父物体和变换
                int[] itemIconIds = new int[] { ResourceIds.ICON_GOLD, ResourceIds.ICON_MAGICAL_STONE, ResourceIds.ICON_HOLY_WATER };
                int[] coinNums = new int[] { 1000, 500, 250 };
                (item.itemLogic as ItemsInfoItem)?.SetData(itemIconIds, coinNums);
            }

            // 创建 TimeInfoItem
            if (timePrefab != null)
            {
                var item = SpawnItem<UIItemObject>(timePrefab, varLeft1Arr[2]);
                SetupItemTransform(item.gameObject, varLeft1Arr[2]); // ? 设置父物体和变换
                (item.itemLogic as TimeInfoItem)?.SetData("第一章：序幕", 15, "2024-01-15 10:30");
            }

            Log.Info("本地存档 Item 全部创建完成");
        }
        catch (System.Exception ex)
        {
            Log.Error($"加载本地存档 Item 失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 加载并创建云端存档 Item（UniTask 版本）
    /// </summary>
    private async void LoadAndCreateCloudItems()
    {
        try
        {
            // 并行加载三个预制体
            var playerPrefabTask = ResourceExtension.LoadPrefabAsync(ResourceIds.PREFAB_PLAYER_INFO_ITEM);
            var itemsPrefabTask = ResourceExtension.LoadPrefabAsync(ResourceIds.PREFAB_ITEMS_INFO_ITEM);
            var timePrefabTask = ResourceExtension.LoadPrefabAsync(ResourceIds.PREFAB_TIME_INFO_ITEM);

            // 等待所有预制体加载完成
            var (playerPrefab, itemsPrefab, timePrefab) = await UniTask.WhenAll(playerPrefabTask, itemsPrefabTask, timePrefabTask);

            // 创建 PlayerInfoItem
            if (playerPrefab != null)
            {
                var item = SpawnItem<UIItemObject>(playerPrefab, varLeft2Arr[0]);
                SetupItemTransform(item.gameObject, varLeft2Arr[0]); // ? 设置父物体和变换
                (item.itemLogic as PlayerInfoItem)?.SetData("云端玩家", "法师", 30);
            }

            // 创建 ItemsInfoItem
            if (itemsPrefab != null)
            {
                var item = SpawnItem<UIItemObject>(itemsPrefab, varLeft2Arr[1]);
                SetupItemTransform(item.gameObject, varLeft2Arr[1]); // ? 设置父物体和变换
                int[] itemIconIds = new int[] { ResourceIds.ICON_GOLD, ResourceIds.ICON_MAGICAL_STONE, ResourceIds.ICON_HOLY_WATER };
                int[] coinNums = new int[] { 2000, 800, 400 };
                (item.itemLogic as ItemsInfoItem)?.SetData(itemIconIds, coinNums);
            }

            // 创建 TimeInfoItem
            if (timePrefab != null)
            {
                var item = SpawnItem<UIItemObject>(timePrefab, varLeft2Arr[2]);
                SetupItemTransform(item.gameObject, varLeft2Arr[2]); // ? 设置父物体和变换
                (item.itemLogic as TimeInfoItem)?.SetData("第二章：冒险", 20, "2024-01-12 15:45");
            }

            Log.Info("云端存档 Item 全部创建完成");
        }
        catch (System.Exception ex)
        {
            Log.Error($"加载云端存档 Item 失败: {ex.Message}");
        }
    }

    #region 辅助方法

    /// <summary>
    /// 设置 Item 的父物体和 Transform
    /// </summary>
    private void SetupItemTransform(GameObject itemGo, Transform parent)
    {
        if (itemGo == null || parent == null) return;

        var rectTransform = itemGo.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // 先设置父物体
            rectTransform.SetParent(parent, false);

            // 重置 Transform
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;

            // 设置锚点和偏移，如果需要
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
    #endregion

    #endregion
}
