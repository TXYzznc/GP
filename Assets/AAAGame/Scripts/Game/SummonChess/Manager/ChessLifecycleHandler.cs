using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 棋子生命周期处理器 - 监听棋子生成，处理 HP 归零的死亡流程
/// 职责：当棋子 HP 降至 0 时，驱动死亡状态、从追踪器注销、并延迟销毁
/// 挂载于 SummonChessManager 所在的 GameObject 上
/// </summary>
public class ChessLifecycleHandler : MonoBehaviour
{
    private void Start()
    {
        if (SummonChessManager.Instance != null)
        {
            SummonChessManager.Instance.OnChessSpawned += OnChessSpawned;
        }
        else
        {
            DebugEx.Error(nameof(ChessLifecycleHandler), "SummonChessManager.Instance 为 null，无法订阅 OnChessSpawned");
        }
    }

    private void OnDestroy()
    {
        if (SummonChessManager.Instance != null)
        {
            SummonChessManager.Instance.OnChessSpawned -= OnChessSpawned;
        }
    }

    private void OnChessSpawned(ChessEntity entity)
    {
        if (entity == null)
            return;

        entity.Attribute.OnHpChanged += (oldHp, newHp) =>
        {
            if (newHp <= 0 && oldHp > 0)
            {
                HandleChessDeath(entity);
            }
        };
    }

    private void HandleChessDeath(ChessEntity entity)
    {
        if (entity == null)
            return;

        DebugEx.Log(nameof(ChessLifecycleHandler), $"棋子死亡标记: chessId={entity.ChessId}, name={entity.Config?.Name}");

        // 1. 玩家棋子（Camp=0）标记死亡
        if (entity.Camp == 0)
        {
            string instanceId = ChessDeploymentTracker.Instance?.GetInstanceIdByEntity(entity);
            if (!string.IsNullOrEmpty(instanceId))
            {
                ChessDeploymentTracker.Instance.MarkChessDead(instanceId);
                DebugEx.Log(nameof(ChessLifecycleHandler),
                    $"已标记棋子死亡: instanceId={instanceId}, chessId={entity.ChessId}");
            }
        }

        // 2. 从实时追踪器注销（避免被AI或自动系统继续操作）
        if (CombatEntityTracker.Instance != null)
            CombatEntityTracker.Instance.UnregisterChess(entity);

        // 3. 驱动棋子进入死亡状态（播放死亡动画）
        entity.ChangeState(ChessState.Dead);

        // 4. 禁用碰撞器（不再受到伤害等物理效果）
        var colliders = entity.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        DebugEx.Log(nameof(ChessLifecycleHandler),
            $"已禁用碰撞器，棋子现在处于死亡状态，等待战斗结束统一处理");

        // ✅ 不调用 DestroyAfterDelay，不销毁 GameObject
        // 战斗结束时统一销毁所有死亡棋子
    }

    private async UniTaskVoid DestroyAfterDelay(ChessEntity entity, float delay)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delay));

        if (entity != null && SummonChessManager.Instance != null)
        {
            SummonChessManager.Instance.DestroyChess(entity);
        }
    }
}
