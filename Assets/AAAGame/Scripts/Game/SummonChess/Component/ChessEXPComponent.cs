using System;
using UnityEngine;

/// <summary>
/// 棋子经验值组件，挂在 ChessEntity 同一 GameObject 上
/// </summary>
public class ChessEXPComponent : MonoBehaviour
{
    public int CurrentEXP { get; private set; }

    /// <summary>经验值变化事件 (oldEXP, newEXP)</summary>
    public event Action<int, int> OnEXPChanged;

    public void SetEXP(int exp)
    {
        int old = CurrentEXP;
        CurrentEXP = Mathf.Max(0, exp);
        if (old != CurrentEXP)
            OnEXPChanged?.Invoke(old, CurrentEXP);
    }

    public void AddEXP(int amount)
    {
        if (amount <= 0)
        {
            DebugEx.Log("ChessEXPComponent", $"❌ AddEXP 被调用但amount无效: {amount}");
            return;
        }
        int old = CurrentEXP;
        CurrentEXP += amount;
        DebugEx.Log("ChessEXPComponent", $"📊 AddEXP 成功: 棋子={gameObject.name}, 增加经验={amount}, {old} → {CurrentEXP}");
        if (OnEXPChanged != null)
        {
            DebugEx.Log("ChessEXPComponent", $"✅ OnEXPChanged 触发，订阅者数={OnEXPChanged.GetInvocationList().Length}");
            OnEXPChanged?.Invoke(old, CurrentEXP);
        }
        else
        {
            DebugEx.Warning("ChessEXPComponent", $"⚠️ OnEXPChanged 为null，没有订阅者");
        }
    }
}
