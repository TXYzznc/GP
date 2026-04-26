using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class OverworldUI : UIFormBase
{
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        InitializeMapItems();

        if (varCloseBtn != null)
        {
            varCloseBtn.onClick.AddListener(OnCloseButtonClicked);
        }

        // 打开地图时解锁鼠标
        PlayerInputManager.Instance.SetCursorLock(false);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);

        if (varCloseBtn != null)
        {
            varCloseBtn.onClick.RemoveListener(OnCloseButtonClicked);
        }

        // 关闭地图时锁定鼠标
        PlayerInputManager.Instance.SetCursorLock(true);
    }

    /// <summary>
    /// 初始化所有地图项
    /// 遍历所有 varMapItemUI 引用，初始化它们
    /// </summary>
    private void InitializeMapItems()
    {
        // 收集所有地图项
        GameObject[] mapItemGameObjects = new GameObject[]
        {
            varMapItemUI1,
            varMapItemUI2,
            varMapItemUI3,
            varMapItemUI4,
            varMapItemUI5,
            varMapItemUI6,
            varMapItemUI7,
        };

        int validCount = 0;
        foreach (var mapItemGO in mapItemGameObjects)
        {
            if (mapItemGO == null)
                continue;

            validCount++;
            MapItemUI mapItem = mapItemGO.GetComponent<MapItemUI>();
            if (mapItem == null)
            {
                DebugEx.Warning("OverworldUI", $"GameObject {mapItemGO.name} 缺少 MapItemUI 组件");
                continue;
            }

            // 从 Inspector 中读取 SceneId
            var sceneIdComponent = mapItemGO.GetComponent<MapItemSceneIdHolder>();
            if (sceneIdComponent != null)
            {
                mapItem.Initialize(sceneIdComponent.SceneId);
                DebugEx.Success(
                    "OverworldUI",
                    $"地图项初始化完成，SceneId: {sceneIdComponent.SceneId}"
                );
            }
            else
            {
                DebugEx.Warning(
                    "OverworldUI",
                    $"MapItemUI {mapItemGO.name} 缺少 MapItemSceneIdHolder 组件"
                );
            }
        }

        DebugEx.Log("OverworldUI", $"初始化完成，共 {validCount} 个地图项");
    }

    private void OnCloseButtonClicked()
    {
        GF.UI.CloseUIForm(this.UIForm);
    }
}
