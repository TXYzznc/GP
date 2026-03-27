using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameExtension;
using UnityEngine;
using UnityGameFramework.Runtime;

public class ChessStateUIWorldManager : SingletonBase<ChessStateUIWorldManager>
{
    public static bool HasInstance => Instance != null;

    [SerializeField]
    private int m_PrefabResourceId = ResourceIds.PREFAB_SUMMON_CHESS_STATE_UI;

    [SerializeField]
    private int m_PrewarmCount = 8;

    private GameObject m_Prefab;
    private readonly Queue<SummonChessStateUI> m_Pool = new();
    private readonly Dictionary<int, SummonChessStateUI> m_Active = new();

    private bool m_InCombat;
    private bool m_Subscribed;

    public static async UniTask EnsureExistsAsync(int prewarmCount = -1)
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("ChessStateUIWorldManager");
            go.AddComponent<ChessStateUIWorldManager>();
            DontDestroyOnLoad(go);
        }

        if (prewarmCount > 0)
        {
            Instance.m_PrewarmCount = Mathf.Max(Instance.m_PrewarmCount, prewarmCount);
        }

        await Instance.EnsureInitializedAsync();
    }

    public async UniTask EnterCombatAsync()
    {
        m_InCombat = true;

        await EnsureInitializedAsync();
        Subscribe();

        if (SummonChessManager.Instance != null)
        {
            var all = SummonChessManager.Instance.GetAllChess();
            for (int i = 0; i < all.Count; i++)
            {
                TryAttach(all[i]);
            }
        }
    }

    public void LeaveCombat()
    {
        m_InCombat = false;
        Unsubscribe();
        ReleaseAllActive();
    }

    private async UniTask EnsureInitializedAsync()
    {
        await EnsurePrefabLoadedAsync();
        Prewarm(m_PrewarmCount);
    }

    private async UniTask EnsurePrefabLoadedAsync()
    {
        if (m_Prefab != null)
            return;

        m_Prefab = await ResourceExtension.LoadPrefabAsync(m_PrefabResourceId);
        if (m_Prefab == null)
        {
            DebugEx.ErrorModule(
                "ChessStateUIWorldManager",
                $"加载状态UI预制体失败 ConfigId={m_PrefabResourceId}"
            );
        }
    }

    private void Subscribe()
    {
        if (m_Subscribed)
            return;

        if (SummonChessManager.Instance != null)
        {
            SummonChessManager.Instance.OnChessSpawned += OnChessSpawned;
            SummonChessManager.Instance.OnChessDestroyed += OnChessDestroyed;
            m_Subscribed = true;
        }
    }

    private void Unsubscribe()
    {
        if (!m_Subscribed)
            return;

        if (SummonChessManager.Instance != null)
        {
            SummonChessManager.Instance.OnChessSpawned -= OnChessSpawned;
            SummonChessManager.Instance.OnChessDestroyed -= OnChessDestroyed;
        }

        m_Subscribed = false;
    }

    private void OnChessSpawned(ChessEntity owner)
    {
        TryAttach(owner);
    }

    private void OnChessDestroyed(ChessEntity owner)
    {
        Detach(owner);
    }

    private void TryAttach(ChessEntity owner)
    {
        if (!m_InCombat || owner == null)
            return;

        int key = owner.InstanceId;
        if (m_Active.ContainsKey(key))
            return;

        var profile = owner.GetComponent<IChessStateUIProfile>();
        if (profile == null)
            return;

        var ui = GetFromPool();
        if (ui == null)
            return;

        ui.gameObject.SetActive(true);
        ui.Bind(owner);
        ui.SetFollowTarget(owner.transform, Vector3.zero);
        ui.SetBillboard(true);
        profile.Apply(ui, owner);

        m_Active[key] = ui;
    }

    private void Detach(ChessEntity owner)
    {
        if (owner == null)
            return;

        int key = owner.InstanceId;
        if (!m_Active.TryGetValue(key, out var ui) || ui == null)
        {
            m_Active.Remove(key);
            return;
        }

        ui.Unbind();
        ui.gameObject.SetActive(false);
        ui.transform.SetParent(transform, false);

        m_Active.Remove(key);
        m_Pool.Enqueue(ui);
    }

    private SummonChessStateUI GetFromPool()
    {
        while (m_Pool.Count > 0 && m_Pool.Peek() == null)
        {
            m_Pool.Dequeue();
        }

        if (m_Pool.Count > 0)
        {
            var ui = m_Pool.Dequeue();
            if (ui != null)
            {
                ui.transform.SetParent(transform, false);
            }
            return ui;
        }

        return CreateInstance();
    }

    private SummonChessStateUI CreateInstance()
    {
        if (m_Prefab == null)
            return null;

        var go = Instantiate(m_Prefab, transform);
        var ui = go.GetComponent<SummonChessStateUI>();
        if (ui == null)
        {
            ui = go.GetComponentInChildren<SummonChessStateUI>(true);
        }

        if (ui == null)
        {
            DebugEx.ErrorModule(
                "ChessStateUIWorldManager",
                "状态UI预制体缺少 SummonChessStateUI 脚本"
            );
            Destroy(go);
            return null;
        }

        go.SetActive(false);
        return ui;
    }

    private void Prewarm(int count)
    {
        if (count <= 0)
            return;
        if (m_Prefab == null)
            return;

        int need = count - m_Pool.Count;
        for (int i = 0; i < need; i++)
        {
            var ui = CreateInstance();
            if (ui == null)
                break;
            m_Pool.Enqueue(ui);
        }
    }

    private void ReleaseAllActive()
    {
        if (m_Active.Count == 0)
            return;

        var keys = new List<int>(m_Active.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            int key = keys[i];
            if (!m_Active.TryGetValue(key, out var ui) || ui == null)
            {
                m_Active.Remove(key);
                continue;
            }

            ui.Unbind();
            ui.gameObject.SetActive(false);
            ui.transform.SetParent(transform, false);
            m_Pool.Enqueue(ui);
            m_Active.Remove(key);
        }
    }
}
