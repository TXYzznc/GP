using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 账号数据编辑面板 - 用于快速修改游戏存档数据
/// </summary>
[ToolHubItem("数据工具/账号数据编辑器", "快速修改游戏存档数据，支持批量编辑和保存", 50)]
public class AccountDataEditorPanel : IToolHubPanel
{
    #region 字段

    private string m_DataPath = "";
    private Vector2 m_LeftScrollPos = Vector2.zero;
    private Vector2 m_RightScrollPos = Vector2.zero;

    private List<AccountInfo> m_Accounts = new List<AccountInfo>();
    private int m_SelectedAccountIndex = -1;
    private int m_SelectedSaveIndex = -1;

    private bool m_IsLoading = false;
    private string m_StatusMessage = "";
    private int m_TabIndex = 0;

    #endregion

    #region 数据结构

    private class AccountInfo
    {
        public string AccountId;
        public string DirectoryPath;
        public List<SaveInfo> Saves = new List<SaveInfo>();
    }

    private class SaveInfo
    {
        public string SaveId;
        public string SaveName;
        public string FilePath;
        public PlayerSaveData SaveData;
        public bool IsDirty = false;
        public DateTime LastModifyTime;
    }

    #endregion

    #region IToolHubPanel 实现

    public void OnEnable()
    {
        m_DataPath = EditorPrefs.GetString("AccountDataEditor_LastPath", "");
    }

    public void OnDisable()
    {
        if (!string.IsNullOrEmpty(m_DataPath))
        {
            EditorPrefs.SetString("AccountDataEditor_LastPath", m_DataPath);
        }
    }

    public void OnDestroy() { }

    public void OnGUI()
    {
        EditorGUILayout.LabelField("账号数据编辑工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        DrawPathSelector();
        EditorGUILayout.Space();

        if (!string.IsNullOrEmpty(m_DataPath) && Directory.Exists(m_DataPath))
        {
            EditorGUILayout.BeginHorizontal();

            // 左栏：账号和存档列表
            EditorGUILayout.BeginVertical(GUILayout.Width(350));
            DrawLeftPanel();
            EditorGUILayout.EndVertical();

            // 右栏：数据编辑
            EditorGUILayout.BeginVertical();
            DrawRightPanel();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        DrawStatusBar();
    }

    #endregion

    #region UI 绘制

    private void DrawPathSelector()
    {
        EditorGUILayout.LabelField("数据路径", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        m_DataPath = EditorGUILayout.TextField("路径:", m_DataPath);

        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("选择存档数据目录", m_DataPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                m_DataPath = selectedPath;
                RefreshAccountList();
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "选择包含账号文件夹的目录（如 PlayerSaves 文件夹）",
            MessageType.Info
        );
    }

    private void DrawLeftPanel()
    {
        EditorGUILayout.LabelField("账号和存档", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("刷新", GUILayout.Width(60)))
        {
            RefreshAccountList();
        }
        EditorGUILayout.EndHorizontal();

        if (m_Accounts.Count == 0)
        {
            EditorGUILayout.HelpBox("未找到账号数据", MessageType.Warning);
            return;
        }

        m_LeftScrollPos = EditorGUILayout.BeginScrollView(m_LeftScrollPos);

        // 账号列表
        for (int i = 0; i < m_Accounts.Count; i++)
        {
            var account = m_Accounts[i];
            bool isSelected = m_SelectedAccountIndex == i;

            EditorGUI.BeginChangeCheck();
            isSelected = EditorGUILayout.ToggleLeft(
                $"账号 {account.AccountId} ({account.Saves.Count})",
                isSelected
            );
            if (EditorGUI.EndChangeCheck())
            {
                m_SelectedAccountIndex = isSelected ? i : -1;
                m_SelectedSaveIndex = -1;
            }

            // 存档列表
            if (isSelected)
            {
                EditorGUI.indentLevel++;
                for (int j = 0; j < account.Saves.Count; j++)
                {
                    var save = account.Saves[j];
                    bool saveSelected = m_SelectedSaveIndex == j;

                    string timeStr = save.LastModifyTime.ToString("MM-dd HH:mm");
                    string label = $"{save.SaveName} [{timeStr}]{(save.IsDirty ? " *" : "")}";

                    EditorGUI.BeginChangeCheck();
                    saveSelected = EditorGUILayout.ToggleLeft(label, saveSelected);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_SelectedSaveIndex = saveSelected ? j : -1;
                        m_TabIndex = 0;
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawRightPanel()
    {
        if (m_SelectedAccountIndex < 0 || m_SelectedAccountIndex >= m_Accounts.Count)
        {
            EditorGUILayout.HelpBox("请选择一个存档", MessageType.Info);
            return;
        }

        var account = m_Accounts[m_SelectedAccountIndex];
        if (m_SelectedSaveIndex < 0 || m_SelectedSaveIndex >= account.Saves.Count)
        {
            EditorGUILayout.HelpBox("请选择一个存档", MessageType.Info);
            return;
        }

        var save = account.Saves[m_SelectedSaveIndex];

        EditorGUILayout.LabelField($"编辑存档: {save.SaveName}", EditorStyles.boldLabel);

        // Tab 页签
        string[] tabs = { "基础", "货币", "召唤师", "位置", "卡牌", "其他" };
        m_TabIndex = GUILayout.Toolbar(m_TabIndex, tabs);

        m_RightScrollPos = EditorGUILayout.BeginScrollView(m_RightScrollPos);

        if (save.SaveData != null)
        {
            EditorGUI.BeginChangeCheck();

            switch (m_TabIndex)
            {
                case 0:
                    DrawBasicTab(save);
                    break;
                case 1:
                    DrawCurrencyTab(save);
                    break;
                case 2:
                    DrawSummonerTab(save);
                    break;
                case 3:
                    DrawPositionTab(save);
                    break;
                case 4:
                    DrawCardTab(save);
                    break;
                case 5:
                    DrawOtherTab(save);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                save.IsDirty = true;
            }
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        DrawSaveButtons(save);
    }

    private void DrawBasicTab(SaveInfo save)
    {
        var data = save.SaveData;
        EditorGUILayout.LabelField("基础信息", EditorStyles.boldLabel);
        data.SaveId = EditorGUILayout.TextField("存档ID", data.SaveId);
        data.SaveName = EditorGUILayout.TextField("存档名称", data.SaveName);
        data.GlobalLevel = EditorGUILayout.IntField("等级", data.GlobalLevel);
        data.CurrentExp = EditorGUILayout.IntField("经验值", data.CurrentExp);
        data.CreateTime = EditorGUILayout.DoubleField("创建时间", data.CreateTime);
        data.LastPlayTime = EditorGUILayout.DoubleField("最后游玩时间", data.LastPlayTime);
    }

    private void DrawCurrencyTab(SaveInfo save)
    {
        var data = save.SaveData;
        EditorGUILayout.LabelField("货币", EditorStyles.boldLabel);
        data.Gold = EditorGUILayout.IntField("金币", data.Gold);
        data.MagicalStone = EditorGUILayout.IntField("灵石", data.MagicalStone);
        data.HolyWater = EditorGUILayout.IntField("圣水", data.HolyWater);
    }

    private void DrawSummonerTab(SaveInfo save)
    {
        var data = save.SaveData;
        EditorGUILayout.LabelField("召唤师信息", EditorStyles.boldLabel);
        data.CurrentSummonerId = EditorGUILayout.IntField("当前召唤师ID", data.CurrentSummonerId);
        data.SummonerPhases = EditorGUILayout.IntField("召唤师阶段", data.SummonerPhases);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("已解锁召唤师", EditorStyles.boldLabel);
        if (data.OwnedUnitCardIds != null)
        {
            EditorGUILayout.LabelField($"单位卡数量: {data.OwnedUnitCardIds.Count}");
        }
    }

    private void DrawPositionTab(SaveInfo save)
    {
        var data = save.SaveData;
        EditorGUILayout.LabelField("位置信息", EditorStyles.boldLabel);
        data.PlayerPos = EditorGUILayout.Vector3Field("玩家位置", data.PlayerPos);
        data.CurrentSceneId = EditorGUILayout.IntField("当前场景ID", data.CurrentSceneId);
    }

    private void DrawCardTab(SaveInfo save)
    {
        var data = save.SaveData;
        EditorGUILayout.LabelField("卡牌信息", EditorStyles.boldLabel);

        // 策略卡
        EditorGUILayout.LabelField("策略卡 (ID列表)", EditorStyles.boldLabel);
        if (data.OwnedStrategyCardIds != null)
        {
            EditorGUILayout.LabelField($"数量: {data.OwnedStrategyCardIds.Count}");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("添加", GUILayout.Width(60)))
            {
                data.OwnedStrategyCardIds.Add(0);
            }
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < data.OwnedStrategyCardIds.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                data.OwnedStrategyCardIds[i] = EditorGUILayout.IntField(
                    $"[{i}]",
                    data.OwnedStrategyCardIds[i]
                );
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    data.OwnedStrategyCardIds.RemoveAt(i);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space();

        // 单位卡
        EditorGUILayout.LabelField("单位卡 (ID列表)", EditorStyles.boldLabel);
        if (data.OwnedUnitCardIds != null)
        {
            EditorGUILayout.LabelField($"数量: {data.OwnedUnitCardIds.Count}");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("添加", GUILayout.Width(60)))
            {
                data.OwnedUnitCardIds.Add(0);
            }
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < data.OwnedUnitCardIds.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                data.OwnedUnitCardIds[i] = EditorGUILayout.IntField(
                    $"[{i}]",
                    data.OwnedUnitCardIds[i]
                );
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    data.OwnedUnitCardIds.RemoveAt(i);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space();

        // 科技
        EditorGUILayout.LabelField("科技 (ID列表)", EditorStyles.boldLabel);
        if (data.UnlockedTechIds != null)
        {
            EditorGUILayout.LabelField($"数量: {data.UnlockedTechIds.Count}");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("添加", GUILayout.Width(60)))
            {
                data.UnlockedTechIds.Add(0);
            }
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < data.UnlockedTechIds.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                data.UnlockedTechIds[i] = EditorGUILayout.IntField(
                    $"[{i}]",
                    data.UnlockedTechIds[i]
                );
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    data.UnlockedTechIds.RemoveAt(i);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space();

        // 技能
        EditorGUILayout.LabelField("技能 (ID列表)", EditorStyles.boldLabel);
        if (data.PlayerSkillIds != null)
        {
            EditorGUILayout.LabelField($"数量: {data.PlayerSkillIds.Count}");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("添加", GUILayout.Width(60)))
            {
                data.PlayerSkillIds.Add(0);
            }
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < data.PlayerSkillIds.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                data.PlayerSkillIds[i] = EditorGUILayout.IntField($"[{i}]", data.PlayerSkillIds[i]);
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    data.PlayerSkillIds.RemoveAt(i);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space();

        data.InventoryCapacity = EditorGUILayout.IntField("背包容量", data.InventoryCapacity);
    }

    private void DrawOtherTab(SaveInfo save)
    {
        var data = save.SaveData;
        EditorGUILayout.LabelField("其他信息", EditorStyles.boldLabel);
        data.HasCompletedTutorial = EditorGUILayout.Toggle("教程完成", data.HasCompletedTutorial);

        if (data.CompletedQuestIds != null)
        {
            EditorGUILayout.LabelField($"完成任务数: {data.CompletedQuestIds.Count}");
        }
        if (data.DiscoveredItemIds != null)
        {
            EditorGUILayout.LabelField($"发现物品数: {data.DiscoveredItemIds.Count}");
        }
        if (data.DiscoveredEnemyIds != null)
        {
            EditorGUILayout.LabelField($"发现敌人数: {data.DiscoveredEnemyIds.Count}");
        }
    }

    private void DrawSaveButtons(SaveInfo save)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(!save.IsDirty);
        if (GUILayout.Button("保存修改", GUILayout.Height(30)))
        {
            SaveSaveData(save);
        }
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("重新加载", GUILayout.Height(30)))
        {
            LoadSaveData(save);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawStatusBar()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("状态", EditorStyles.boldLabel);

        if (m_IsLoading)
        {
            EditorGUILayout.HelpBox("正在加载...", MessageType.Info);
        }
        else if (!string.IsNullOrEmpty(m_StatusMessage))
        {
            EditorGUILayout.HelpBox(m_StatusMessage, MessageType.None);
        }
    }

    #endregion

    #region 数据操作

    private void RefreshAccountList()
    {
        m_IsLoading = true;
        m_Accounts.Clear();
        m_SelectedAccountIndex = -1;
        m_SelectedSaveIndex = -1;

        try
        {
            if (!Directory.Exists(m_DataPath))
            {
                m_StatusMessage = "路径不存在";
                return;
            }

            var accountDirs = Directory.GetDirectories(m_DataPath);

            foreach (var accountDir in accountDirs)
            {
                string accountId = Path.GetFileName(accountDir);
                var account = new AccountInfo { AccountId = accountId, DirectoryPath = accountDir };

                var saveFiles = Directory.GetFiles(accountDir, "Save_*.json");
                foreach (var saveFile in saveFiles)
                {
                    string saveId = Path.GetFileNameWithoutExtension(saveFile).Replace("Save_", "");
                    var save = new SaveInfo
                    {
                        SaveId = saveId,
                        SaveName = $"存档_{saveId.Substring(0, 8)}",
                        FilePath = saveFile,
                    };

                    LoadSaveData(save);
                    account.Saves.Add(save);
                }

                if (account.Saves.Count > 0)
                {
                    m_Accounts.Add(account);
                }
            }

            m_StatusMessage = $"已加载 {m_Accounts.Count} 个账号";
        }
        catch (Exception ex)
        {
            m_StatusMessage = $"加载失败: {ex.Message}";
            Debug.LogError($"AccountDataEditor 加载失败: {ex}");
        }
        finally
        {
            m_IsLoading = false;
        }
    }

    private void LoadSaveData(SaveInfo save)
    {
        try
        {
            if (File.Exists(save.FilePath))
            {
                string json = File.ReadAllText(save.FilePath, System.Text.Encoding.UTF8);
                save.SaveData = JsonUtility.FromJson<PlayerSaveData>(json);

                if (save.SaveData != null)
                {
                    save.SaveName = save.SaveData.SaveName;
                    save.IsDirty = false;
                }

                save.LastModifyTime = File.GetLastWriteTime(save.FilePath);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"加载存档失败 {save.FilePath}: {ex}");
        }
    }

    private void SaveSaveData(SaveInfo save)
    {
        try
        {
            if (save.SaveData == null)
            {
                m_StatusMessage = "存档数据为空";
                return;
            }

            string json = JsonUtility.ToJson(save.SaveData, true);
            File.WriteAllText(save.FilePath, json, System.Text.Encoding.UTF8);

            save.IsDirty = false;
            save.LastModifyTime = DateTime.Now;
            m_StatusMessage = $"存档已保存: {save.SaveName}";
            Debug.Log($"存档已保存: {save.FilePath}");
        }
        catch (Exception ex)
        {
            m_StatusMessage = $"保存失败: {ex.Message}";
            Debug.LogError($"保存存档失败: {ex}");
        }
    }

    #endregion
}
