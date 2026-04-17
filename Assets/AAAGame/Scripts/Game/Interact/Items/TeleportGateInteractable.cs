using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 传送阵可交互对象
/// 交互时打开地图界面（OverworldUI），显示所有可传送的场景
/// </summary>
public class TeleportGateInteractable : InteractableBase
{
    public override int Priority => 1;
    public override int InteractAnimIndex => -1;

    protected override void Awake()
    {
        base.Awake();
        interactionTip = "进入传送阵";
    }

    public override bool CanInteract(GameObject player)
    {
        // 传送阵始终可以交互
        return true;
    }

    public override void OnInteract(GameObject player)
    {
        // 打开地图UI（注意：OverworldUI 需要先添加到 UITable，分配 UI ID）
        // GF.UI.OpenUIForm(UIViews.OverworldUI);
        Log.Warning("TeleportGateInteractable: 需要先将 OverworldUI 添加到 UITable，然后取消注释上一行");
    }
}
