using UnityEngine;
using UnityGameFramework.Runtime;
using Cysharp.Threading.Tasks;

/// <summary>
/// 传送阵可交互对象
/// 支持两种类型：
/// 1. 基地传送阵 - 直接回到基地（SceneTable ID=1）
/// 2. 普通传送阵 - 打开地图UI，选择传送目标
/// </summary>
public class TeleportGateInteractable : InteractableBase
{
    public enum TeleportType
    {
        ToBase = 0,      // 回到基地
        AnyWhere = 1     // 可去任何地方
    }

    [SerializeField] private TeleportType m_TeleportType = TeleportType.AnyWhere;

    public override int Priority => 1;
    public override int InteractAnimIndex => -1;

    protected override void Awake()
    {
        base.Awake();
        interactionTip = m_TeleportType == TeleportType.ToBase ? "回到基地" : "进入传送阵";
    }

    public override bool CanInteract(GameObject player)
    {
        // 传送阵始终可以交互
        return true;
    }

    public override void OnInteract(GameObject player)
    {
        if (m_TeleportType == TeleportType.ToBase)
        {
            TeleportToBase();
        }
        else
        {
            OpenMapUI();
        }
    }

    private void TeleportToBase()
    {
        var sceneTable = GF.DataTable.GetDataTable<SceneTable>();
        if (sceneTable == null || !sceneTable.HasDataRow(1))
        {
            Log.Error("TeleportGateInteractable: SceneTable 中不存在 ID=1 的基地场景");
            return;
        }

        var baseScene = sceneTable.GetDataRow(1);
        Log.Info($"TeleportGateInteractable: 触发传送门结算 -> {baseScene.SceneName}");

        // 触发结算流程而非直接加载场景
        // 结算系统会处理：数据收集 -> UI显示 -> 异步加载场景 -> 状态转换 -> 奖励应用
        SettlementManager.Instance.TriggerSettlementAsync(baseScene.SceneName, SettlementTriggerSource.Teleport).Forget();
    }

    private void OpenMapUI()
    {
        GF.UI.OpenUIForm(UIViews.OverworldUI);
    }
}
