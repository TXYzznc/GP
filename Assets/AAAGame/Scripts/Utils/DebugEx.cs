using System.Diagnostics;
using UnityEngine;

/// <summary>
/// 增强版 Debug 工具类
/// 支持彩色输出、条件编译、模块标签等功能
/// </summary>
public static class DebugEx
{
    #region 颜色常量

    public static class Color
    {
        public const string Red = "#FF0000";
        public const string Green = "#00FF00";
        public const string Blue = "#0000FF";
        public const string Yellow = "#FFFF00";
        public const string Cyan = "#00FFFF";
        public const string Magenta = "#FF00FF";
        public const string Orange = "#FFA500";
        public const string Purple = "#800080";
        public const string Pink = "#FFC0CB";
        public const string White = "#FFFFFF";
        public const string Gray = "#808080";
        public const string LightGreen = "#90EE90";
        public const string LightBlue = "#ADD8E6";
    }

    #endregion

    #region 配置

    /// <summary>
    /// 是否启用日志输出（Release 版本可设为 false）
    /// </summary>
    public static bool EnableLog = true;

    /// <summary>
    /// 是否启用警告输出
    /// </summary>
    public static bool EnableWarning = true;

    /// <summary>
    /// 是否启用错误输出
    /// </summary>
    public static bool EnableError = true;

    #endregion

    #region Log 方法

    /// <summary>
    /// 普通日志
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message, string color = "")
    {
        if (!EnableLog)
            return;
        UnityEngine.Debug.Log(message);
    }

    /// <summary>
    /// 带颜色的日志
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogColor(object message, string color)
    {
        if (!EnableLog)
            return;
        UnityEngine.Debug.Log($"<color={color}>{message}</color>");
    }

    /// <summary>
    /// 带模块标签的日志
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogModule(string module, object message)
    {
        if (!EnableLog)
            return;
        UnityEngine.Debug.Log($"[{module}] {message}");
    }

    /// <summary>
    /// 带模块标签和颜色的日志
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogModule(string module, object message, string color)
    {
        if (!EnableLog)
            return;
        UnityEngine.Debug.Log($"<color={color}>[{module}] {message}</color>");
    }

    /// <summary>
    /// 带 Context 的日志（可在 Console 中点击定位到对象）
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message, Object context)
    {
        if (!EnableLog)
            return;
        UnityEngine.Debug.Log(message, context);
    }

    #endregion

    #region Warning 方法

    /// <summary>
    /// 警告日志
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Warning(object message, string color = "")
    {
        if (!EnableWarning)
            return;
        UnityEngine.Debug.LogWarning(message);
    }

    /// <summary>
    /// 带颜色的警告日志
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void WarningColor(object message, string color)
    {
        if (!EnableWarning)
            return;
        UnityEngine.Debug.LogWarning($"<color={color}>{message}</color>");
    }

    /// <summary>
    /// 带模块标签的警告日志
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void WarningModule(string module, object message)
    {
        if (!EnableWarning)
            return;
        UnityEngine.Debug.LogWarning($"[{module}] {message}");
    }

    /// <summary>
    /// 带模块标签和颜色的警告日志
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void WarningModule(string module, object message, string color)
    {
        if (!EnableWarning)
            return;
        UnityEngine.Debug.LogWarning($"<color={color}>[{module}] {message}</color>");
    }

    /// <summary>
    /// 带 Context 的警告日志
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Warning(object message, Object context)
    {
        if (!EnableWarning)
            return;
        UnityEngine.Debug.LogWarning(message, context);
    }

    #endregion

    #region Error 方法

    /// <summary>
    /// 错误日志
    /// </summary>
    public static void Error(object message, string color = "")
    {
        if (!EnableError)
            return;
        UnityEngine.Debug.LogError(message);
    }

    /// <summary>
    /// 带颜色的错误日志
    /// </summary>
    public static void ErrorColor(object message, string color)
    {
        if (!EnableError)
            return;
        UnityEngine.Debug.LogError($"<color={color}>{message}</color>");
    }

    /// <summary>
    /// 带模块标签的错误日志
    /// </summary>
    public static void ErrorModule(string module, object message)
    {
        if (!EnableError)
            return;
        UnityEngine.Debug.LogError($"[{module}] {message}");
    }

    /// <summary>
    /// 带模块标签和颜色的错误日志
    /// </summary>
    public static void ErrorModule(string module, object message, string color)
    {
        if (!EnableError)
            return;
        UnityEngine.Debug.LogError($"<color={color}>[{module}] {message}</color>");
    }

    /// <summary>
    /// 带 Context 的错误日志
    /// </summary>
    public static void Error(object message, Object context)
    {
        if (!EnableError)
            return;
        UnityEngine.Debug.LogError(message, context);
    }

    #endregion

    #region 便捷方法

    /// <summary>
    /// 成功日志（绿色）
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Success(object message)
    {
        LogColor(message, Color.Green);
    }

    /// <summary>
    /// 成功日志（绿色，带模块标签）
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Success(string module, object message)
    {
        LogModule(module, message, Color.Green);
    }

    /// <summary>
    /// 失败日志（红色）
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Fail(object message)
    {
        LogColor(message, Color.Red);
    }

    /// <summary>
    /// 失败日志（红色，带模块标签）
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Fail(string module, object message)
    {
        LogModule(module, message, Color.Red);
    }

    /// <summary>
    /// 信息日志（青色）
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Info(object message)
    {
        LogColor(message, Color.Cyan);
    }

    /// <summary>
    /// 信息日志（青色，带模块标签）
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Info(string module, object message)
    {
        LogModule(module, message, Color.Cyan);
    }

    /// <summary>
    /// 分隔线
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Separator(string title = "")
    {
        if (!EnableLog)
            return;
        string line = string.IsNullOrEmpty(title)
            ? "========================================"
            : $"========== {title} ==========";
        UnityEngine.Debug.Log($"<color={Color.Yellow}>{line}</color>");
    }

    #endregion

    #region 断言

    /// <summary>
    /// 断言（条件为 false 时输出错误）
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Error($"断言失败: {message}");
        }
    }

    /// <summary>
    /// 断言（条件为 false 时输出错误，带模块标签）
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Assert(bool condition, string module, string message)
    {
        if (!condition)
        {
            ErrorModule(module, $"断言失败: {message}");
        }
    }

    #endregion
}
