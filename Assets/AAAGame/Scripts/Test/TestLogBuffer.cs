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

    private const int MAX_LOG_COUNT = 1000; // 最多保留1000条日志

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

    /// <summary>日志条目</summary>
    public struct LogEntry
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
    private bool m_IsListening = false;
    private bool m_IsInitialized = false;

    #endregion

    #region 属性

    public IReadOnlyList<LogEntry> Logs => m_Logs.AsReadOnly();
    public bool IsListening => m_IsListening;

    #endregion

    #region 公共方法

    /// <summary>初始化</summary>
    private void Initialize()
    {
        if (!m_IsInitialized)
        {
            m_Logs = new List<LogEntry>();
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

    /// <summary>清空日志</summary>
    public void ClearLogs()
    {
        m_Logs.Clear();
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
                writer.WriteLine();

                foreach (var log in m_Logs)
                {
                    writer.WriteLine(log.ToString());
                }

                writer.WriteLine();
                writer.WriteLine(new string('=', 80));
                writer.WriteLine($"共 {m_Logs.Count} 条日志");
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
        int count = 0;
        foreach (var log in m_Logs)
        {
            if (log.Type == type)
                count++;
        }
        return count;
    }

    #endregion

    #region 私有方法

    private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        // 添加日志条目
        var entry = new LogEntry
        {
            Time = DateTime.Now,
            Message = condition,
            Type = type
        };

        m_Logs.Add(entry);

        // 如果超过最大数量，移除最旧的日志
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
