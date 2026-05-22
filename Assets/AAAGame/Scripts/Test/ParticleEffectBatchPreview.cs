using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 粒子特效批量预览 - 场景版本
/// 在场景中以网格方式显示特效，支持批次切换和播放控制
///
/// 快捷键：
/// A / ← - 上一批
/// D / → - 下一批
/// Space - 播放/暂停
/// R - 重启
/// </summary>
public class ParticleEffectBatchPreview : MonoBehaviour
{
    [Header("特效配置")]
    [SerializeField]
    [Tooltip("特效文件夹路径（相对于 Assets，勿需添加 Assets/ 前缀）")]
    private string m_FolderPath = "特效/ARPG Effects";

    [Header("网格参数")]
    [SerializeField]
    [Tooltip("网格列数")]
    private int m_GridColumns = 3;

    [SerializeField]
    [Tooltip("网格行数")]
    private int m_GridRows = 3;

    [SerializeField]
    [Tooltip("格子间距")]
    private float m_CellSpacing = 2f;

    [SerializeField]
    [Tooltip("特效最大尺寸")]
    private float m_MaxEffectSize = 1.5f;

    [Header("调试")]
    [SerializeField]
    [Tooltip("显示调试信息")]
    private bool m_ShowDebugUI = true;

    private List<GameObject> m_EffectPrefabs = new List<GameObject>();
    private List<GameObject> m_CurrentBatchInstances = new List<GameObject>();
    private List<ParticleSystem> m_AllParticleSystems = new List<ParticleSystem>();

    private int m_CurrentBatch = 0;
    private bool m_IsPlaying = true;
    private GameObject m_ContainerRoot;

    private void Start()
    {
        LoadEffectPrefabs();
        CreateContainerRoot();
        DisplayBatch(0);

        DebugEx.Success(nameof(ParticleEffectBatchPreview),
            $"特效预览已启动，加载 {m_EffectPrefabs.Count} 个特效");
    }

    private void Update()
    {
        HandleInput();
    }

    private void OnGUI()
    {
        if (!m_ShowDebugUI) return;

        GUILayout.BeginArea(new Rect(10, 10, 500, 500));
        GUILayout.BeginVertical("box");

        GUILayout.Label("<b><size=14>特效批量预览</size></b>", GetRichTextStyle());
        GUILayout.Space(5);

        // 批次信息
        int batchSize = m_GridColumns * m_GridRows;
        int totalBatches = Mathf.CeilToInt((float)m_EffectPrefabs.Count / batchSize);
        int startIndex = m_CurrentBatch * batchSize + 1;
        int endIndex = Mathf.Min((m_CurrentBatch + 1) * batchSize, m_EffectPrefabs.Count);

        GUILayout.Label($"<b>批次信息</b>", GetHeaderStyle());
        GUILayout.Label($"当前: {m_CurrentBatch + 1}/{totalBatches}");
        GUILayout.Label($"显示: {startIndex}-{endIndex}/{m_EffectPrefabs.Count}");
        GUILayout.Space(8);

        // 批次控制按钮
        GUILayout.Label($"<b>批次控制</b>", GetHeaderStyle());
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("◄ 上一批", GUILayout.Height(30)))
        {
            PreviousBatch();
        }
        if (GUILayout.Button("下一批 ►", GUILayout.Height(30)))
        {
            NextBatch();
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(8);

        // 状态信息
        GUILayout.Label($"<b>播放状态</b>", GetHeaderStyle());
        GUILayout.Label($"状态: {(m_IsPlaying ? "<color=green>播放中</color>" : "<color=red>已暂停</color>")}");
        GUILayout.Label($"粒子系统: {m_AllParticleSystems.Count}");
        GUILayout.Space(8);

        // 播放控制按钮
        GUILayout.Label($"<b>播放控制</b>", GetHeaderStyle());
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(m_IsPlaying ? "⏸ 暂停" : "▶ 播放", GUILayout.Height(30)))
        {
            TogglePlayPause();
        }
        if (GUILayout.Button("↻ 重启", GUILayout.Height(30)))
        {
            RestartAllParticles();
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(8);

        // 网格参数
        GUILayout.Label($"<b>网格参数</b>", GetHeaderStyle());
        GUILayout.Label($"布局: {m_GridColumns}×{m_GridRows}");
        GUILayout.Label($"特效大小: {m_MaxEffectSize:F2}");
        GUILayout.Label($"文件夹: {m_FolderPath}");
        GUILayout.Space(8);

        // 快捷键说明
        GUILayout.Label($"<b>快捷键</b>", GetHeaderStyle());
        GUILayout.Label("A/← - 上一批  |  D/→ - 下一批");
        GUILayout.Label("Space - 播放/暂停  |  R - 重启");
        GUILayout.Label("H - 隐藏此面板");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void HandleInput()
    {
        // 上一批
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            PreviousBatch();
        }

        // 下一批
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            NextBatch();
        }

        // 播放/暂停
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TogglePlayPause();
        }

        // 重启
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartAllParticles();
        }

        // 隐藏/显示UI
        if (Input.GetKeyDown(KeyCode.H))
        {
            m_ShowDebugUI = !m_ShowDebugUI;
        }
    }

    private void LoadEffectPrefabs()
    {
        m_EffectPrefabs.Clear();

#if UNITY_EDITOR
        // 编辑器模式：使用 AssetDatabase
        string searchPath = m_FolderPath.StartsWith("Assets/") ? m_FolderPath : "Assets/" + m_FolderPath;
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { searchPath });

        foreach (string guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (prefab != null && prefab.GetComponentInChildren<ParticleSystem>() != null)
            {
                m_EffectPrefabs.Add(prefab);
            }
        }

        if (m_EffectPrefabs.Count == 0)
        {
            DebugEx.Warning(nameof(ParticleEffectBatchPreview),
                $"未找到特效，请检查路径: {searchPath}");
        }
#else
    // 运行时模式：使用 Resources
    GameObject[] prefabs = Resources.LoadAll<GameObject>(m_FolderPath);
    foreach (var prefab in prefabs)
    {
        if (prefab.GetComponentInChildren<ParticleSystem>() != null)
        {
            m_EffectPrefabs.Add(prefab);
        }
    }
#endif

        DebugEx.Success(nameof(ParticleEffectBatchPreview),
            $"加载特效完成，共 {m_EffectPrefabs.Count} 个");
    }

    private void CreateContainerRoot()
    {
        m_ContainerRoot = new GameObject("ParticleEffectBatchRoot");
        m_ContainerRoot.transform.SetParent(transform, false);
    }

    private void DisplayBatch(int batchIndex)
    {
        // 清理旧特效
        ClearCurrentBatch();

        if (m_EffectPrefabs.Count == 0)
        {
            DebugEx.Warning(nameof(ParticleEffectBatchPreview), "没有特效预制体可显示");
            return;
        }

        m_CurrentBatch = Mathf.Clamp(batchIndex, 0, Mathf.CeilToInt((float)m_EffectPrefabs.Count / (m_GridColumns * m_GridRows)) - 1);

        int batchSize = m_GridColumns * m_GridRows;
        int startIndex = m_CurrentBatch * batchSize;
        int endIndex = Mathf.Min(startIndex + batchSize, m_EffectPrefabs.Count);

        float cellSize = m_MaxEffectSize + m_CellSpacing;
        float gridWidth = m_GridColumns * cellSize;
        float gridHeight = m_GridRows * cellSize;

        int localIndex = 0;
        for (int i = startIndex; i < endIndex; i++)
        {
            int row = localIndex / m_GridColumns;
            int col = localIndex % m_GridColumns;

            // 计算位置
            float x = col * cellSize - gridWidth / 2 + cellSize / 2;
            float z = row * cellSize - gridHeight / 2 + cellSize / 2;
            Vector3 cellPos = new Vector3(x, 0, z);

            // 创建格子容器
            GameObject cellObj = new GameObject($"Cell_{row}_{col}");
            cellObj.transform.SetParent(m_ContainerRoot.transform, false);
            cellObj.transform.localPosition = cellPos;

            // 实例化特效
            GameObject effectInstance = Instantiate(m_EffectPrefabs[i]);
            effectInstance.name = m_EffectPrefabs[i].name;
            effectInstance.transform.SetParent(cellObj.transform, false);
            effectInstance.transform.localPosition = Vector3.zero;
            effectInstance.transform.localRotation = Quaternion.identity;

            // 自动缩放
            ScaleEffectToBounds(effectInstance);

            m_CurrentBatchInstances.Add(effectInstance);

            // 收集粒子系统
            var particles = effectInstance.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                m_AllParticleSystems.Add(ps);
            }

            localIndex++;
        }

        // 播放所有粒子系统
        PlayAllParticles();

        DebugEx.Success(nameof(ParticleEffectBatchPreview),
            $"显示批次 {m_CurrentBatch + 1}，共 {m_CurrentBatchInstances.Count} 个特效");
    }

    private void ClearCurrentBatch()
    {
        // 删除容器下所有的Cell对象（包括特效实例）
        if (m_ContainerRoot != null)
        {
            foreach (Transform child in m_ContainerRoot.transform)
            {
                Destroy(child.gameObject);
            }
        }

        m_CurrentBatchInstances.Clear();
        m_AllParticleSystems.Clear();
    }

    private void ScaleEffectToBounds(GameObject effectObj)
    {
        ParticleSystem[] particles = effectObj.GetComponentsInChildren<ParticleSystem>();
        if (particles.Length == 0) return;

        float maxSize = 0;
        foreach (var particle in particles)
        {
            ParticleSystemRenderer renderer = particle.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                Bounds bounds = renderer.bounds;
                float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                maxSize = Mathf.Max(maxSize, size);
            }
        }

        if (maxSize > 0)
        {
            float scale = m_MaxEffectSize / maxSize;
            effectObj.transform.localScale = Vector3.one * scale;
        }
    }

    private void PlayAllParticles()
    {
        foreach (var ps in m_AllParticleSystems)
        {
            if (ps != null)
            {
                ps.Stop();
                ps.Play();
            }
        }
        m_IsPlaying = true;
    }

    private void PauseAllParticles()
    {
        foreach (var ps in m_AllParticleSystems)
        {
            if (ps != null)
                ps.Pause();
        }
        m_IsPlaying = false;
    }

    private void RestartAllParticles()
    {
        foreach (var ps in m_AllParticleSystems)
        {
            if (ps != null)
            {
                ps.Stop();
                ps.Play();
            }
        }
        m_IsPlaying = true;
        DebugEx.Log(nameof(ParticleEffectBatchPreview), "已重启所有粒子系统");
    }

    private void TogglePlayPause()
    {
        if (m_IsPlaying)
        {
            PauseAllParticles();
            DebugEx.Log(nameof(ParticleEffectBatchPreview), "已暂停");
        }
        else
        {
            PlayAllParticles();
            DebugEx.Log(nameof(ParticleEffectBatchPreview), "已播放");
        }
    }

    private void PreviousBatch()
    {
        if (m_CurrentBatch > 0)
        {
            DisplayBatch(m_CurrentBatch - 1);
        }
    }

    private void NextBatch()
    {
        int batchSize = m_GridColumns * m_GridRows;
        int totalBatches = Mathf.CeilToInt((float)m_EffectPrefabs.Count / batchSize);
        if (m_CurrentBatch < totalBatches - 1)
        {
            DisplayBatch(m_CurrentBatch + 1);
        }
    }

    private GUIStyle GetRichTextStyle()
    {
        var style = new GUIStyle(GUI.skin.label);
        style.richText = true;
        style.alignment = TextAnchor.MiddleLeft;
        return style;
    }

    private GUIStyle GetHeaderStyle()
    {
        var style = new GUIStyle(GUI.skin.label);
        style.richText = true;
        style.fontStyle = FontStyle.Bold;
        return style;
    }
}
