using System;
using UnityEngine;

/// <summary>
/// 诊断脚本：测量 Play Mode 启动的各个阶段耗时
/// 帮助识别 "Completing Domain" 卡顿的瓶颈
/// </summary>
public class StartupPerformanceProfiler
{
    private static long s_DomainLoadStartTime = 0L;

    // 记录各阶段的时间戳
    private static long s_BeforeSceneLoadTime = 0L;
    private static long s_AfterSceneLoadTime = 0L;

    static StartupPerformanceProfiler()
    {
        // 在最早的时刻（静态构造函数）记录时间
        s_DomainLoadStartTime = DateTime.UtcNow.Ticks;
        Debug.Log($"[Startup] [Domain Load Start] {GetElapsedMs(s_DomainLoadStartTime)}ms");
    }

    /// <summary>
    /// 场景加载之前
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void OnBeforeSceneLoad()
    {
        s_BeforeSceneLoadTime = DateTime.UtcNow.Ticks;
        long elapsedMs = GetElapsedMs(s_DomainLoadStartTime);

        Debug.Log($"[Startup] ┌─ [✓ BeforeSceneLoad] {elapsedMs}ms");
        Debug.Log($"[Startup] │  Bottleneck: Static constructors, [InitializeOnLoad], type initialization");

        if (elapsedMs > 3000)
        {
            Debug.LogWarning($"[Startup] │  ⚠️ SLOW: Domain load took {elapsedMs}ms (expected < 1000ms)");
        }
    }

    /// <summary>
    /// 场景加载之后
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void OnAfterSceneLoad()
    {
        s_AfterSceneLoadTime = DateTime.UtcNow.Ticks;
        long totalElapsedMs = GetElapsedMs(s_DomainLoadStartTime);
        long sceneLoadMs = GetElapsedMs(s_BeforeSceneLoadTime);

        Debug.Log($"[Startup] [✓ AfterSceneLoad] {totalElapsedMs}ms total, {sceneLoadMs}ms for scene load");
        Debug.Log($"[Startup]     → Bottleneck: Scene initialization, resource loading, Awake/Start calls");

        if (sceneLoadMs > 2000)
        {
            Debug.LogWarning($"[Startup] ⚠️ SLOW: Scene load took {sceneLoadMs}ms (expected < 1000ms)");
        }
    }

    /// <summary>
    /// 在游戏逻辑初始化后调用（可选）
    /// </summary>
    public static void OnGameReady()
    {
        long totalElapsedMs = GetElapsedMs(s_DomainLoadStartTime);

        Debug.Log("[Startup] ");
        Debug.Log("[Startup] ╔═══════════════════════════════════════════════════════╗");
        Debug.Log("[Startup] ║          STARTUP PERFORMANCE DETAILED REPORT          ║");
        Debug.Log("[Startup] ╚═══════════════════════════════════════════════════════╝");
        Debug.Log($"[Startup] Total Startup Time: {totalElapsedMs}ms");
        Debug.Log("[Startup] ");
        Debug.Log("[Startup] 📊 TIMELINE:");

        if (s_BeforeSceneLoadTime > 0)
        {
            long domainLoadMs = GetElapsedMs(s_DomainLoadStartTime, s_BeforeSceneLoadTime);
            Debug.Log($"[Startup] ├─ Domain Load:          {domainLoadMs}ms");
            PrintPerformanceLevel("│  ", domainLoadMs, 1000);
        }

        if (s_AfterSceneLoadTime > 0)
        {
            long sceneLoadMs = GetElapsedMs(s_BeforeSceneLoadTime, s_AfterSceneLoadTime);
            Debug.Log($"[Startup] ├─ Scene Load:           {sceneLoadMs}ms");
            PrintPerformanceLevel("│  ", sceneLoadMs, 1500);
        }

        long logicInitMs = GetElapsedMs(s_AfterSceneLoadTime);
        Debug.Log($"[Startup] └─ Logic Initialization:  {logicInitMs}ms (Procedures, DataTable, UI setup)");
        PrintPerformanceLevel("   ", logicInitMs, 2000);

        Debug.Log("[Startup] ");
        Debug.Log("[Startup] 📈 OVERALL PERFORMANCE:");

        // 性能评级
        if (totalElapsedMs < 2000)
        {
            Debug.Log("[Startup] Status: ✅ EXCELLENT (< 2s) - Optimal performance");
        }
        else if (totalElapsedMs < 5000)
        {
            Debug.Log("[Startup] Status: 🟡 GOOD (< 5s) - Acceptable performance");
        }
        else if (totalElapsedMs < 10000)
        {
            Debug.Log("[Startup] Status: 🟠 FAIR (5-10s) - Noticeable startup delay");
        }
        else
        {
            Debug.Log("[Startup] Status: 🔴 SLOW (> 10s) - Consider optimization:");
            Debug.Log("[Startup]   → Disable Auto Refresh in Preferences");
            Debug.Log("[Startup]   → Use DebugCommentTool to disable Debug logs");
            Debug.Log("[Startup]   → Profile with Profiler to find bottlenecks");
        }

        Debug.Log("[Startup] ");
    }

    /// <summary>
    /// 输出性能等级指示器
    /// </summary>
    private static void PrintPerformanceLevel(string prefix, long timeMs, long threshold)
    {
        if (timeMs > threshold * 2)
            Debug.LogWarning($"[Startup] {prefix}⚠️ SLOW (> {threshold * 2}ms)");
        else if (timeMs > threshold)
            Debug.Log($"[Startup] {prefix}⚡ MODERATE ({threshold}-{threshold * 2}ms)");
        else
            Debug.Log($"[Startup] {prefix}✓ FAST (< {threshold}ms)");
    }

    /// <summary>
    /// 计算从起点到现在的耗时（毫秒）
    /// </summary>
    private static long GetElapsedMs(long startTicks)
    {
        return (DateTime.UtcNow.Ticks - startTicks) / 10000;
    }

    /// <summary>
    /// 计算两个时间点之间的耗时（毫秒）
    /// </summary>
    private static long GetElapsedMs(long startTicks, long endTicks)
    {
        return (endTicks - startTicks) / 10000;
    }
}