#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 测试日志缓冲器 - 仅在编辑器模式下使用
/// 收集和导出游戏日志，用于在 GameTestWindow 中显示
/// </summary>
public class TestLogBuffer : ScriptableObject
{
    #region 常量

    private const int MAX_LOG_COUNT = 5000; // 最多保留5000条日志

    #endregion

    #region 单例

    private static TestLogBuffer s_Instance;

    public static TestLogBuffer Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = CreateInstance<TestLogBuffer>();
                s_Instance.Initialize();
            }
            return s_Instance;
        }
    }

    #endregion

    #region 嵌套类

    private struct LogEntry
    {
        public DateTime Time;
        public string Message;
        public LogType Type;

        public override string ToString()
        {
            return $"[{Time:HH:mm:ss.fff}] [{Type}] {Message}";
        }
    }

    #endregion

    #region 字段

    private List<LogEntry> m_Logs = new List<LogEntry>();
    private int m_TotalLogCount = 0;
    private int m_LogCount = 0;
    private int m_WarningCount = 0;
    private int m_ErrorCount = 0;
    private int m_ExceptionCount = 0;
    private bool m_IsListening = false;
    private bool m_IsInitialized = false;

    #endregion

    #region 属性

    public int TotalLogCount => m_TotalLogCount;
    public int LogCount => m_LogCount;
    public int WarningCount => m_WarningCount;
    public int ErrorCount => m_ErrorCount;
    public int ExceptionCount => m_ExceptionCount;
    public bool IsListening => m_IsListening;

    #endregion

    #region 公共方法

    /// <summary>初始化</summary>
    private void Initialize()
    {
        if (!m_IsInitialized)
        {
            m_Logs = new List<LogEntry>();
            m_TotalLogCount = 0;
            m_LogCount = 0;
            m_WarningCount = 0;
            m_ErrorCount = 0;
            m_ExceptionCount = 0;
            m_IsInitialized = true;
        }
    }

    /// <summary>启动日志捕获</summary>
    public void StartListening()
    {
        if (m_IsListening)
            return;

        m_IsListening = true;
        Application.logMessageReceived += OnLogMessageReceived;
        Debug.Log("[TestLogBuffer] 日志捕获已启动");
    }

    /// <summary>停止日志捕获</summary>
    public void StopListening()
    {
        if (!m_IsListening)
            return;

        m_IsListening = false;
        Application.logMessageReceived -= OnLogMessageReceived;
        Debug.Log("[TestLogBuffer] 日志捕获已停止");
    }

    /// <summary>清空日志统计</summary>
    public void ClearLogs()
    {
        m_Logs.Clear();
        m_TotalLogCount = 0;
        m_LogCount = 0;
        m_WarningCount = 0;
        m_ErrorCount = 0;
        m_ExceptionCount = 0;
    }

    /// <summary>导出日志到文件</summary>
    public string ExportLogsToFile()
    {
        string fileName = $"GameTest_Log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        try
        {
            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                writer.WriteLine($"游戏测试日志 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine(new string('=', 80));
                writer.WriteLine($"统计: 总{m_TotalLogCount} | Log{m_LogCount} | Warning{m_WarningCount} | Error{m_ErrorCount} | Exception{m_ExceptionCount}");
                writer.WriteLine(new string('=', 80));
                writer.WriteLine();

                foreach (var log in m_Logs)
                {
                    writer.WriteLine(log.ToString());
                }

                writer.WriteLine();
                writer.WriteLine(new string('=', 80));
                writer.WriteLine($"共 {m_Logs.Count} 条日志记录");
            }

            Debug.Log($"[TestLogBuffer] 日志已导出到: {filePath}");
            return filePath;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TestLogBuffer] 导出日志失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>获取指定类型的日志数量</summary>
    public int GetLogCountByType(LogType type)
    {
        return type switch
        {
            LogType.Log => m_LogCount,
            LogType.Warning => m_WarningCount,
            LogType.Error => m_ErrorCount,
            LogType.Exception => m_ExceptionCount,
            _ => 0
        };
    }

    #endregion

    #region 私有方法

    private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        // 存储日志（供导出使用）
        var entry = new LogEntry
        {
            Time = DateTime.Now,
            Message = condition,
            Type = type
        };
        m_Logs.Add(entry);

        // 维护计数统计（供 UI 显示）
        m_TotalLogCount++;

        switch (type)
        {
            case LogType.Log:
                m_LogCount++;
                break;
            case LogType.Warning:
                m_WarningCount++;
                break;
            case LogType.Error:
                m_ErrorCount++;
                break;
            case LogType.Exception:
                m_ExceptionCount++;
                break;
        }

        // 限制日志条数（防止内存溢出）
        if (m_Logs.Count > MAX_LOG_COUNT)
        {
            m_Logs.RemoveAt(0);
        }
    }

    private void OnDestroy()
    {
        StopListening();
    }

    #endregion
}

#endif
