using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GM 面板（开发调试用）
/// C 键切换显示/隐藏，使用 IMGUI 渲染，无需 Prefab 配置
/// </summary>
public class GMPanelManager : MonoBehaviour
{
    #region 单例

    private static GMPanelManager s_Instance;
    public static GMPanelManager Instance => s_Instance;

    #endregion

    #region 常量

    private const float WINDOW_WIDTH = 700f;
    private const float WINDOW_HEIGHT = 550f;
    private const int FONT_SIZE = 14;

    #endregion

    #region 私有字段

    private bool m_IsVisible = false;
    private int m_ActiveTab = 0;
    private Rect m_WindowRect;

    // 物品 Tab
    private List<ItemTable> m_AllItems;
    private Vector2 m_ItemScrollPos;
    private string m_ItemSearchText = "";
    private string m_ItemCountText = "1";

    // 解锁 Tab（无需缓存，实时读取）

    // 局内棋子 Tab
    private Vector2 m_ChessScrollPos;

    private static readonly string[] TAB_NAMES = { "物品管理", "解锁管理", "局内棋子" };

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_Instance = this;
        DontDestroyOnLoad(gameObject);

        m_WindowRect = new Rect(
            (Screen.width - WINDOW_WIDTH) / 2f,
            (Screen.height - WINDOW_HEIGHT) / 2f,
            WINDOW_WIDTH,
            WINDOW_HEIGHT
        );
    }

    private void OnDestroy()
    {
        if (s_Instance == this)
            s_Instance = null;
    }

    private void Update()
    {
        if (PlayerInputManager.Instance == null)
            return;

        if (PlayerInputManager.Instance.GMPanelToggleTriggered)
            TogglePanel();
    }

    private void OnGUI()
    {
        if (!m_IsVisible)
            return;

        GUI.skin.font = GUI.skin.font; // 保持默认字体

        // 调整全局字号
        var oldFontSize = GUI.skin.label.fontSize;
        var oldBtnFontSize = GUI.skin.button.fontSize;
        var oldTxtFontSize = GUI.skin.textField.fontSize;
        GUI.skin.label.fontSize = FONT_SIZE;
        GUI.skin.button.fontSize = FONT_SIZE;
        GUI.skin.textField.fontSize = FONT_SIZE;

        m_WindowRect = GUI.Window(9999, m_WindowRect, DrawWindow, "GM 面板 [C键关闭]");

        GUI.skin.label.fontSize = oldFontSize;
        GUI.skin.button.fontSize = oldBtnFontSize;
        GUI.skin.textField.fontSize = oldTxtFontSize;
    }

    #endregion

    #region 面板控制

    private void TogglePanel()
    {
        m_IsVisible = !m_IsVisible;

        if (m_IsVisible)
        {
            PlayerInputManager.Instance.RequestMouseUnlock();
            // 切换到物品 Tab 时懒加载数据
            if (m_ActiveTab == 0)
                EnsureItemsLoaded();
        }
        else
        {
            PlayerInputManager.Instance.RequestMouseLock();
        }
    }

    #endregion

    #region IMGUI 窗口

    private void DrawWindow(int windowId)
    {
        GUILayout.BeginVertical();

        // Tab 切换栏
        int newTab = GUILayout.Toolbar(m_ActiveTab, TAB_NAMES, GUILayout.Height(30));
        if (newTab != m_ActiveTab)
        {
            m_ActiveTab = newTab;
            if (m_ActiveTab == 0)
                EnsureItemsLoaded();
        }

        GUILayout.Space(5);

        // 内容区
        switch (m_ActiveTab)
        {
            case 0: DrawItemTab(); break;
            case 1: DrawUnlockTab(); break;
            case 2: DrawChessTab(); break;
        }

        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0, 0, WINDOW_WIDTH, 20));
    }

    #endregion

    #region Tab 1：物品管理

    private void EnsureItemsLoaded()
    {
        if (m_AllItems != null)
            return;

        m_AllItems = new List<ItemTable>();
        var table = GF.DataTable.GetDataTable<ItemTable>();
        if (table == null)
        {
            DebugEx.Warning("GMPanelManager", "ItemTable 未加载");
            return;
        }

        foreach (var row in table.GetAllDataRows())
        {
            // 跳过虚拟物品（金币/灵石）
            if (row.Type == (int)ItemType.Virtual)
                continue;
            m_AllItems.Add(row);
        }

        DebugEx.Log("GMPanelManager", $"物品列表加载完成，共 {m_AllItems.Count} 条");
    }

    private void DrawItemTab()
    {
        // 搜索 + 数量
        GUILayout.BeginHorizontal();
        GUILayout.Label("搜索:", GUILayout.Width(40));
        m_ItemSearchText = GUILayout.TextField(m_ItemSearchText, GUILayout.Width(200));
        GUILayout.Space(20);
        GUILayout.Label("数量:", GUILayout.Width(40));
        m_ItemCountText = GUILayout.TextField(m_ItemCountText, GUILayout.Width(60));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        // 表头
        GUILayout.BeginHorizontal();
        GUILayout.Label("名称", GUILayout.Width(180));
        GUILayout.Label("类型", GUILayout.Width(80));
        GUILayout.Label("品质", GUILayout.Width(80));
        GUILayout.Label("操作", GUILayout.Width(80));
        GUILayout.EndHorizontal();

        // 列表
        m_ItemScrollPos = GUILayout.BeginScrollView(m_ItemScrollPos, GUILayout.Height(WINDOW_HEIGHT - 160));

        if (m_AllItems == null)
        {
            GUILayout.Label("ItemTable 未加载");
        }
        else
        {
            string filter = m_ItemSearchText.ToLower();
            foreach (var row in m_AllItems)
            {
                if (!string.IsNullOrEmpty(filter) && !row.Name.ToLower().Contains(filter))
                    continue;

                GUILayout.BeginHorizontal();
                GUILayout.Label(row.Name, GUILayout.Width(180));
                GUILayout.Label(GetItemTypeName(row.Type), GUILayout.Width(80));
                GUILayout.Label(GetRarityName(row.Rarity), GUILayout.Width(80));

                if (GUILayout.Button("+添加", GUILayout.Width(70)))
                    OnAddItemClicked(row.Id);

                GUILayout.EndHorizontal();
            }
        }

        GUILayout.EndScrollView();
    }

    private void OnAddItemClicked(int itemId)
    {
        if (!int.TryParse(m_ItemCountText, out int count) || count <= 0)
            count = 1;

        bool ok = InventoryManager.Instance.AddItem(itemId, count);
        DebugEx.Log("GMPanelManager", ok
            ? $"GM 添加物品 ID={itemId} x{count} 成功"
            : $"GM 添加物品 ID={itemId} x{count} 失败（背包已满或数据不存在）");
    }

    private static string GetItemTypeName(int type)
    {
        return (ItemType)type switch
        {
            ItemType.Consumable => "消耗品",
            ItemType.Quest      => "任务",
            ItemType.Treasure   => "宝物",
            ItemType.Equipment  => "装备",
            _                   => type.ToString()
        };
    }

    private static string GetRarityName(int rarity)
    {
        return (ItemRarity)rarity switch
        {
            ItemRarity.Common    => "普通",
            ItemRarity.Uncommon  => "优良",
            ItemRarity.Rare      => "稀有",
            ItemRarity.Epic      => "史诗",
            ItemRarity.Legendary => "传说",
            _                    => rarity.ToString()
        };
    }

    #endregion

    #region Tab 2：解锁管理

    private void DrawUnlockTab()
    {
        var saveData = PlayerAccountDataManager.Instance?.CurrentSaveData;
        if (saveData == null)
        {
            GUILayout.Label("存档未加载");
            return;
        }

        // 策略卡
        GUILayout.BeginVertical("box");
        GUILayout.Label("── 策略卡 ──");

        var cardTable = GF.DataTable.GetDataTable<CardTable>();
        int totalCards = cardTable != null ? cardTable.Count : 0;
        int ownedCards = saveData.OwnedStrategyCardIds?.Count ?? 0;
        GUILayout.Label($"已解锁：{ownedCards} / {totalCards}");

        if (GUILayout.Button("一键解锁全部策略卡", GUILayout.Height(30)))
            UnlockAllCards(saveData);

        GUILayout.EndVertical();

        GUILayout.Space(10);

        // 棋子
        GUILayout.BeginVertical("box");
        GUILayout.Label("── 棋子 ──");

        var allChessIds = ChessDataManager.Instance.GetAllConfigIds();
        int totalChess = allChessIds.Count;
        int ownedChess = saveData.OwnedUnitCardIds?.Count ?? 0;
        GUILayout.Label($"已解锁：{ownedChess} / {totalChess}");

        if (GUILayout.Button("一键解锁全部棋子", GUILayout.Height(30)))
            UnlockAllChess(allChessIds);

        GUILayout.EndVertical();
    }

    private void UnlockAllCards(PlayerSaveData saveData)
    {
        if (saveData.OwnedStrategyCardIds == null)
        {
            DebugEx.Warning("GMPanelManager", "存档无效");
            return;
        }

        var cardTable = GF.DataTable.GetDataTable<CardTable>();
        if (cardTable == null)
        {
            DebugEx.Warning("GMPanelManager", "CardTable 未加载");
            return;
        }

        int count = 0;
        foreach (var row in cardTable.GetAllDataRows())
        {
            if (!saveData.OwnedStrategyCardIds.Contains(row.Id))
            {
                saveData.OwnedStrategyCardIds.Add(row.Id);
                count++;
            }
        }

        PlayerAccountDataManager.Instance.SaveCurrentSave();
        DebugEx.Success("GMPanelManager", $"GM 解锁策略卡 {count} 张，总计 {saveData.OwnedStrategyCardIds.Count} 张");
    }

    private void UnlockAllChess(List<int> allChessIds)
    {
        int count = 0;
        foreach (var id in allChessIds)
        {
            if (ChessUnlockManager.Instance.UnlockChess(id))
                count++;
        }

        DebugEx.Success("GMPanelManager", $"GM 解锁棋子 {count} 个");
    }

    #endregion

    #region Tab 3：局内棋子管理

    private void DrawChessTab()
    {
        bool inCombat = CombatManager.Instance != null && CombatManager.Instance.IsInCombat;

        if (!inCombat)
        {
            GUILayout.Label("⚠ 仅战斗中有效（当前不在战斗）");
            return;
        }

        // 全体满血按钮
        if (GUILayout.Button("全体满血", GUILayout.Height(30)))
            HealAllChess();

        GUILayout.Space(5);

        // 表头
        GUILayout.BeginHorizontal();
        GUILayout.Label("棋子名称", GUILayout.Width(120));
        GUILayout.Label("HP", GUILayout.Width(200));
        GUILayout.Label("状态", GUILayout.Width(60));
        GUILayout.Label("操作", GUILayout.Width(160));
        GUILayout.EndHorizontal();

        // 棋子列表
        var allies = CombatEntityTracker.Instance.GetAlliesIncludingDead((int)CampType.Player);

        m_ChessScrollPos = GUILayout.BeginScrollView(m_ChessScrollPos, GUILayout.Height(WINDOW_HEIGHT - 180));

        foreach (var chess in allies)
        {
            if (chess == null) continue;

            bool isDead = chess.Attribute.IsDead;
            double hp = chess.Attribute.CurrentHp;
            double maxHp = chess.Attribute.MaxHp;

            GUILayout.BeginHorizontal();

            GUILayout.Label(chess.Config?.Name ?? "未知", GUILayout.Width(120));

            // HP 进度条
            float ratio = maxHp > 0 ? (float)(hp / maxHp) : 0f;
            DrawHpBar(ratio, hp, maxHp);

            GUILayout.Label(isDead ? "死亡" : "存活", GUILayout.Width(60));

            // 满血按钮（存活时显示）
            if (!isDead)
            {
                if (GUILayout.Button("满血", GUILayout.Width(60)))
                    chess.Attribute.SetHp(chess.Attribute.MaxHp);
            }

            // 复活按钮（死亡时显示）
            if (isDead)
            {
                if (GUILayout.Button("复活", GUILayout.Width(60)))
                    ReviveChess(chess);
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
    }

    private void DrawHpBar(float ratio, double hp, double maxHp)
    {
        // 用 GUILayout 模拟进度条
        var rect = GUILayoutUtility.GetRect(200, 18, GUILayout.Width(200));
        GUI.Box(rect, "");
        var fillRect = new Rect(rect.x + 1, rect.y + 1, (rect.width - 2) * Mathf.Clamp01(ratio), rect.height - 2);
        GUI.color = ratio > 0.5f ? Color.green : ratio > 0.2f ? Color.yellow : Color.red;
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(rect, $"  {hp:F0}/{maxHp:F0}");
    }

    private void HealAllChess()
    {
        var allies = CombatEntityTracker.Instance.GetAllies((int)CampType.Player);
        int count = 0;
        foreach (var chess in allies)
        {
            if (chess == null || chess.Attribute.IsDead) continue;
            chess.Attribute.SetHp(chess.Attribute.MaxHp);
            count++;
        }
        DebugEx.Log("GMPanelManager", $"GM 全体满血，共 {count} 个棋子");
    }

    private void ReviveChess(ChessEntity chess)
    {
        if (chess == null) return;

        // 1. 重新启用 Collider（在 SetHp 之前）
        var colliders = chess.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
            col.enabled = true;

        // 2. 恢复满血
        chess.Attribute.SetHp(chess.Attribute.MaxHp);

        // 3. 切换出死亡状态
        chess.ChangeState(ChessState.Idle);

        // 4. 重新加入实体追踪器
        CombatEntityTracker.Instance.ReviveChess(chess);

        // 5. 清除部署追踪中的死亡标记
        string instanceId = ChessDeploymentTracker.Instance?.GetInstanceIdByEntity(chess);
        if (!string.IsNullOrEmpty(instanceId))
            ChessDeploymentTracker.Instance.MarkChessAlive(instanceId);

        DebugEx.Success("GMPanelManager", $"GM 复活棋子：{chess.Config?.Name}");
    }

    #endregion
}
