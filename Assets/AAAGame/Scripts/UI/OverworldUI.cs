using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class OverworldUI : UIFormBase
{
    private const string MapItemUIName = "MapItemUI";

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        InitializeMapItems();

        if (varCloseBtn != null)
        {
            varCloseBtn.onClick.AddListener(OnCloseButtonClicked);
        }
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);

        if (varCloseBtn != null)
        {
            varCloseBtn.onClick.RemoveListener(OnCloseButtonClicked);
        }
    }

    /// <summary>
    /// 初始化所有地图项
    /// 遍历 varMapItemUI 容器中的所有 MapItemUI 子对象，初始化它们
    /// </summary>
    private void InitializeMapItems()
    {
        if (varMapItemUI == null)
        {
            Log.Error("OverworldUI: varMapItemUI 未设置");
            return;
        }

        // 获取所有 MapItemUI 子对象
        MapItemUI[] mapItems = varMapItemUI.GetComponentsInChildren<MapItemUI>(includeInactive: false);

        if (mapItems.Length == 0)
        {
            Log.Warning("OverworldUI: 未找到任何 MapItemUI 子对象");
            return;
        }

        // 初始化每个地图项
        foreach (var mapItem in mapItems)
        {
            // 从 Inspector 中读取 SceneId（需要在 MapItemUI 上添加字段）
            var sceneIdComponent = mapItem.GetComponent<MapItemSceneIdHolder>();
            if (sceneIdComponent != null)
            {
                mapItem.Initialize(sceneIdComponent.SceneId);
            }
            else
            {
                Log.Warning($"OverworldUI: MapItemUI 缺少 MapItemSceneIdHolder 组件");
            }
        }
    }

    private void OnCloseButtonClicked()
    {
        GF.UI.CloseUIForm(this.UIForm);
    }
}