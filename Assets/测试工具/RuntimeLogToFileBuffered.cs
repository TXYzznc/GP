using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using UnityEngine;

public class RuntimeLogToFileBuffered : MonoBehaviour
{
    [Header("Hotkeys")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Q;     // 开始/停止
    [SerializeField] private KeyCode snapshotKey = KeyCode.E;   // 保存快照（复制当前log文件）
    [SerializeField] private KeyCode restartKey = KeyCode.C;    // 重新开一个新文件

    [Header("Write Settings")]
    [SerializeField] private bool autoStart = false;
    [SerializeField] private bool includeStackTraceForErrors = true;

    [Tooltip("每隔多少秒写一次盘（批量写入）")]
    [SerializeField] private float flushInterval = 0.25f;

    [Tooltip("队列累计到多少条就立刻写一次（避免堆太多）")]
    [SerializeField] private int flushBatchSize = 80;

    [Header("File Settings")]
    [SerializeField] private string folderName = "Assets/测试输出日志";
    [SerializeField] private string fileNamePrefix = "unity_log";

    public enum LogSaveLocation
    {
        ProjectRoot_EditorOnly,   // Editor: 项目根目录；Build: 自动回退 persistentDataPath
        PersistentDataPath,
        AppFolder_IfPossible      // Build(桌面端): exe同级；移动端通常回退 persistentDataPath
    }

    [SerializeField] private LogSaveLocation saveLocation = LogSaveLocation.ProjectRoot_EditorOnly;

    private readonly ConcurrentQueue<string> _queue = new();
    private StreamWriter _writer;
    private string _filePath;
    private bool _running;
    private Coroutine _flushCo;

    void Awake()
    {
        if (autoStart) StartLogging();
    }

    void OnEnable()
    {
        // threaded：即使日志从其它线程来，也能收到
        Application.logMessageReceivedThreaded += OnLogThreaded;
    }

    void OnDisable()
    {
        Application.logMessageReceivedThreaded -= OnLogThreaded;
        StopLogging();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (_running) StopLogging();
            else StartLogging();
        }

        if (Input.GetKeyDown(snapshotKey))
        {
            SaveSnapshot();
        }

        if (Input.GetKeyDown(restartKey))
        {
            RestartLogging();
        }
    }

    public void StartLogging()
    {
        if (_running) return;

        var dir = GetLogDirectory();
        Directory.CreateDirectory(dir);

        Debug.Log(dir);

        var time = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _filePath = Path.Combine(dir, $"{fileNamePrefix}_{time}.txt");

        _writer = new StreamWriter(_filePath, append: true, new UTF8Encoding(false))
        {
            AutoFlush = false
        };

        _writer.WriteLine($"===== Log Start: {DateTime.Now:yyyy-MM-dd HH:mm:ss} =====");
        _writer.WriteLine($"Unity: {Application.unityVersion}");
        _writer.WriteLine($"Platform: {Application.platform}");
        _writer.WriteLine($"persistentDataPath: {Application.persistentDataPath}");
        _writer.WriteLine();

        _running = true;

        if (_flushCo == null)
            _flushCo = StartCoroutine(FlushLoop());

        // 这条会被写进文件
        Debug.Log($"[RuntimeLogToFileBuffered] Start -> {_filePath}");
    }

    public void StopLogging()
    {
        if (!_running) return;

        _running = false;

        // 先把队列剩余写完
        FlushOnce(force: true);

        try
        {
            _writer?.WriteLine();
            _writer?.WriteLine($"===== Log End: {DateTime.Now:yyyy-MM-dd HH:mm:ss} =====");
            _writer?.Flush();
            _writer?.Dispose();
        }
        catch { /* ignore */ }

        _writer = null;

        if (_flushCo != null)
        {
            StopCoroutine(_flushCo);
            _flushCo = null;
        }

        Debug.Log("[RuntimeLogToFileBuffered] Stop");
    }

    public void RestartLogging()
    {
        StopLogging();
        StartLogging();
    }

    public string GetCurrentFilePath() => _filePath;

    public void SaveSnapshot(string snapshotFileName = null)
    {
        // 注意：Unity不提供读取Console历史的API，所以快照就是复制当前文件
        if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
        {
            Debug.LogWarning("[RuntimeLogToFileBuffered] No log file to snapshot.");
            return;
        }

        // 先把队列刷一下，保证快照尽量完整
        FlushOnce(force: true);

        var dir = Path.GetDirectoryName(_filePath);
        var time = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var name = string.IsNullOrEmpty(snapshotFileName) ? $"snapshot_{time}.txt" : snapshotFileName;
        var dst = Path.Combine(dir, name);

        File.Copy(_filePath, dst, overwrite: true);
        Debug.Log($"[RuntimeLogToFileBuffered] Snapshot saved: {dst}");
    }

    private void OnLogThreaded(string condition, string stackTrace, LogType type)
    {
        if (!_running) return;

        // 组装成一条或多条字符串，入队即可（不要在这里做IO）
        var sb = new StringBuilder(256);
        sb.Append('[').Append(DateTime.Now.ToString("HH:mm:ss.fff")).Append("] ");
        sb.Append('[').Append(type).Append("] ");
        sb.AppendLine(condition);

        if (includeStackTraceForErrors &&
            (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
        {
            if (!string.IsNullOrEmpty(stackTrace))
                sb.AppendLine(stackTrace);
        }

        _queue.Enqueue(sb.ToString());
    }

    private System.Collections.IEnumerator FlushLoop()
    {
        var wait = new WaitForSecondsRealtime(Mathf.Max(0.02f, flushInterval));

        while (true)
        {
            // 没在运行就不写（但协程会被 StopLogging 停掉）
            if (_running)
            {
                // 达到批量阈值就立即刷
                if (_queue.Count >= flushBatchSize)
                    FlushOnce(force: false);
                else
                    FlushOnce(force: false);
            }

            yield return wait;
        }
    }

    private void FlushOnce(bool force)
    {
        if (_writer == null) return;

        int wrote = 0;
        while (_queue.TryDequeue(out var msg))
        {
            _writer.Write(msg);
            wrote++;

            // 非强制情况下，写到 batch size 就停，留给下次
            if (!force && wrote >= flushBatchSize)
                break;
        }

        if (wrote > 0)
            _writer.Flush();
    }

    private string GetLogDirectory()
    {
        string dir;

#if UNITY_EDITOR
        if (saveLocation == LogSaveLocation.ProjectRoot_EditorOnly)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            dir = Path.Combine(projectRoot, folderName);
            return dir;
        }
#endif

        if (saveLocation == LogSaveLocation.PersistentDataPath)
        {
            dir = Path.Combine(Application.persistentDataPath, folderName);
            return dir;
        }

        // AppFolder_IfPossible
        // Editor 下：Application.dataPath 是 Assets，父目录仍是项目根
        // Player 下：Application.dataPath 通常是 xxx_Data，父目录是 exe 所在目录（桌面端可能可写）
        try
        {
            var appFolder = Directory.GetParent(Application.dataPath).FullName;
            dir = Path.Combine(appFolder, folderName);

            // 简单探测能不能写（避免权限问题）
            Directory.CreateDirectory(dir);
            var testFile = Path.Combine(dir, "__write_test.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);

            return dir;
        }
        catch
        {
            // 回退到更可靠的 persistentDataPath
            dir = Path.Combine(Application.persistentDataPath, folderName);
            return dir;
        }
    }
}