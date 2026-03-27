using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using AAAGame.Debug;

namespace AAAGame.Editor.Debug
{
    /// <summary>
    /// 运行时调试控制台 - Editor Window 版本
    /// </summary>
    public class RuntimeDebugConsoleWindow : EditorWindow
    {
        #region 字段

        private string m_InputCommand = "";
        private Vector2 m_ScrollPosition;
        private List<LogEntry> m_LogEntries = new List<LogEntry>();
        private List<string> m_CommandHistory = new List<string>();
        private int m_HistoryIndex = -1;
        private bool m_AutoScroll = true;
        private bool m_FocusInput = false;

        #endregion

        #region 日志条目

        private class LogEntry
        {
            public string Message;
            public string Time;
            public LogType Type;

            public LogEntry(string message, LogType type)
            {
                Message = message;
                Time = System.DateTime.Now.ToString("HH:mm:ss");
                Type = type;
            }
        }

        private enum LogType
        {
            Command,
            Success,
            Error,
            Info
        }

        #endregion

        #region Unity 生命周期

        [MenuItem("工具/调试控制台 &`")]
        public static void ShowWindow()
        {
            var window = GetWindow<RuntimeDebugConsoleWindow>("调试控制台");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            AddLog("调试控制台已启动", LogType.Info);
            AddLog("输入命令格式: ClassName.MethodName(arg1, arg2)", LogType.Info);
            AddLog("示例: GameStateManager.Instance.SwitchToInGame()", LogType.Info);
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawLogArea();
            DrawInputArea();
            HandleKeyboardInput();
        }

        #endregion

        #region UI 绘制

        /// <summary>
        /// 绘制工具栏
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("清空", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                m_LogEntries.Clear();
            }

            m_AutoScroll = GUILayout.Toggle(m_AutoScroll, "自动滚动", EditorStyles.toolbarButton, GUILayout.Width(80));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("帮助", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                ShowHelp();
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制日志区域
        /// </summary>
        private void DrawLogArea()
        {
            // 日志区域
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition, GUILayout.ExpandHeight(true));

            foreach (var entry in m_LogEntries)
            {
                DrawLogEntry(entry);
            }

            // 自动滚动到底部
            if (m_AutoScroll && Event.current.type == EventType.Repaint)
            {
                m_ScrollPosition.y = float.MaxValue;
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制单条日志
        /// </summary>
        private void DrawLogEntry(LogEntry entry)
        {
            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.richText = true;
            style.wordWrap = true;

            Color bgColor = GetLogBackgroundColor(entry.Type);
            
            EditorGUILayout.BeginHorizontal();
            
            // 时间戳
            GUILayout.Label($"[{entry.Time}]", GUILayout.Width(70));
            
            // 消息内容
            EditorGUILayout.SelectableLabel(entry.Message, style, GUILayout.ExpandWidth(true), GUILayout.MinHeight(20));
            
            EditorGUILayout.EndHorizontal();

            // 分隔线
            if (entry.Type == LogType.Command)
            {
                EditorGUILayout.Space(2);
            }
        }

        /// <summary>
        /// 绘制输入区域
        /// </summary>
        private void DrawInputArea()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.SetNextControlName("CommandInput");
            m_InputCommand = EditorGUILayout.TextField(m_InputCommand, GUILayout.ExpandWidth(true));

            // 自动聚焦输入框
            if (m_FocusInput)
            {
                EditorGUI.FocusTextInControl("CommandInput");
                m_FocusInput = false;
            }

            if (GUILayout.Button("执行", GUILayout.Width(60)))
            {
                ExecuteCommand();
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 命令执行

        /// <summary>
        /// 执行命令
        /// </summary>
        private void ExecuteCommand()
        {
            if (string.IsNullOrWhiteSpace(m_InputCommand))
            {
                return;
            }

            // 记录命令
            AddLog($"> {m_InputCommand}", LogType.Command);

            // 添加到历史记录
            m_CommandHistory.Add(m_InputCommand);
            m_HistoryIndex = m_CommandHistory.Count;

            try
            {
                // 解析命令
                ParsedCommand parsedCommand = CommandParser.Parse(m_InputCommand);

                // 执行命令
                string result = CommandExecutor.Execute(parsedCommand);

                // 显示结果
                LogType logType = result.Contains("<color=red>") ? LogType.Error : LogType.Success;
                AddLog(result, logType);
            }
            catch (System.Exception ex)
            {
                AddLog($"<color=red>解析失败: {ex.Message}</color>", LogType.Error);
            }

            // 清空输入
            m_InputCommand = "";
            m_FocusInput = true;
            Repaint();
        }

        #endregion

        #region 键盘输入处理

        /// <summary>
        /// 处理键盘输入
        /// </summary>
        private void HandleKeyboardInput()
        {
            Event e = Event.current;

            if (e.type == EventType.KeyDown)
            {
                // 回车执行命令
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    ExecuteCommand();
                    e.Use();
                }
                // 上箭头 - 上一条命令
                else if (e.keyCode == KeyCode.UpArrow)
                {
                    NavigateHistory(-1);
                    e.Use();
                }
                // 下箭头 - 下一条命令
                else if (e.keyCode == KeyCode.DownArrow)
                {
                    NavigateHistory(1);
                    e.Use();
                }
            }
        }

        /// <summary>
        /// 导航命令历史
        /// </summary>
        private void NavigateHistory(int direction)
        {
            if (m_CommandHistory.Count == 0)
            {
                return;
            }

            m_HistoryIndex += direction;
            m_HistoryIndex = Mathf.Clamp(m_HistoryIndex, 0, m_CommandHistory.Count);

            if (m_HistoryIndex < m_CommandHistory.Count)
            {
                m_InputCommand = m_CommandHistory[m_HistoryIndex];
            }
            else
            {
                m_InputCommand = "";
            }

            m_FocusInput = true;
            Repaint();
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 添加日志
        /// </summary>
        private void AddLog(string message, LogType type)
        {
            m_LogEntries.Add(new LogEntry(message, type));
            Repaint();
        }

        /// <summary>
        /// 获取日志背景颜色
        /// </summary>
        private Color GetLogBackgroundColor(LogType type)
        {
            switch (type)
            {
                case LogType.Command:
                    return new Color(0.3f, 0.3f, 0.4f, 0.3f);
                case LogType.Error:
                    return new Color(0.5f, 0.2f, 0.2f, 0.3f);
                case LogType.Success:
                    return new Color(0.2f, 0.5f, 0.2f, 0.3f);
                default:
                    return Color.clear;
            }
        }

        /// <summary>
        /// 显示帮助信息
        /// </summary>
        private void ShowHelp()
        {
            AddLog("=== 命令格式说明 ===", LogType.Info);
            AddLog("方法调用: ClassName.Instance.MethodName(arg1, arg2)", LogType.Info);
            AddLog("属性访问: ClassName.Instance.PropertyName", LogType.Info);
            AddLog("属性赋值: ClassName.Instance.PropertyName = value", LogType.Info);
            AddLog("私有成员: ClassName.Instance.#PrivateMember", LogType.Info);
            AddLog("", LogType.Info);
            AddLog("=== 示例命令 ===", LogType.Info);
            AddLog("GameStateManager.Instance.SwitchToInGame()", LogType.Info);
            AddLog("Time.timeScale = 0.5", LogType.Info);
            AddLog("GameObject.Find(\"Player\").transform.position = (0, 10, 0)", LogType.Info);
            AddLog("", LogType.Info);
            AddLog("=== 快捷键 ===", LogType.Info);
            AddLog("Alt + ` : 打开/关闭控制台", LogType.Info);
            AddLog("Enter : 执行命令", LogType.Info);
            AddLog("↑/↓ : 浏览命令历史", LogType.Info);
        }

        #endregion
    }
}
