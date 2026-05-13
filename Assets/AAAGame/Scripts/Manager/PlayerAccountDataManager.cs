using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// 玩家账号数据管理器，支持多账号、多存档
/// </summary>
public class PlayerAccountDataManager
{
    // 单例
    private static PlayerAccountDataManager s_Instance;
    public static PlayerAccountDataManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new PlayerAccountDataManager();
            }
            return s_Instance;
        }
    }

    // 当前账号ID（暂时固定，未来由登录系统提供）
    private string m_CurrentAccountId = "000001";

    /// <summary>
    /// 私有构造函数，用于初始化事件监听
    /// </summary>
    private PlayerAccountDataManager()
    {
        // 订阅棋子解锁事件
        if (ChessUnlockManager.Instance != null)
        {
            ChessUnlockManager.Instance.OnChessUnlocked += OnChessUnlocked;
        }
    }

    /// <summary>
    /// 棋子解锁回调
    /// </summary>
    private void OnChessUnlocked(int chessId)
    {
        // 棋子数据已直接存储在 OwnedUnitCardIds 中，无需额外同步
        DebugEx.Log("PlayerAccountDataManager", $"棋子已解锁: {chessId}");
    }

    // 当前玩家存档数据
    private PlayerSaveData m_CurrentSaveData;

    // 当前账号信息
    private PlayerAccountInfo m_CurrentAccountInfo;

    // 文件存储相关常量
    private const string SAVE_ROOT_DIRECTORY = "PlayerSaves";
    private const string ACCOUNT_INFO_FILE = "AccountInfo.json";
    private const string SAVE_FILE_PREFIX = "Save_";
    private const string SAVE_FILE_EXTENSION = ".json";

    // 存档根目录路径（懒加载）
    private string m_SaveRootPath;
    private string SaveRootPath
    {
        get
        {
            if (string.IsNullOrEmpty(m_SaveRootPath))
            {
                m_SaveRootPath = Path.Combine(Application.persistentDataPath, SAVE_ROOT_DIRECTORY);

                if (!Directory.Exists(m_SaveRootPath))
                {
                    Directory.CreateDirectory(m_SaveRootPath);
                    DebugEx.Log("PlayerAccountDataManager", $"创建存档根目录: {m_SaveRootPath}");
                }
            }
            return m_SaveRootPath;
        }
    }

    /// <summary>
    /// 获取当前账号目录路径
    /// </summary>
    private string GetAccountDirectoryPath(string accountId)
    {
        string path = Path.Combine(SaveRootPath, accountId);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            DebugEx.Log("PlayerAccountDataManager", $"创建账号目录: {path}");
        }
        return path;
    }

    /// <summary>
    /// 获取账号信息文件路径
    /// </summary>
    private string GetAccountInfoFilePath(string accountId)
    {
        return Path.Combine(GetAccountDirectoryPath(accountId), ACCOUNT_INFO_FILE);
    }

    /// <summary>
    /// 获取存档文件路径
    /// </summary>
    private string GetSaveFilePath(string accountId, string saveId)
    {
        string fileName = $"{SAVE_FILE_PREFIX}{saveId}{SAVE_FILE_EXTENSION}";
        return Path.Combine(GetAccountDirectoryPath(accountId), fileName);
    }

    /// <summary>
    /// 获取当前存档数据
    /// </summary>
    public PlayerSaveData CurrentSaveData => m_CurrentSaveData;

    /// <summary>
    /// 是否有当前存档
    /// </summary>
    public bool HasSaveData => m_CurrentSaveData != null;

    /// <summary>
    /// 设置当前账号ID（由登录系统调用）
    /// </summary>
    public void SetCurrentAccountId(string accountId)
    {
        m_CurrentAccountId = accountId;
        LoadAccountInfo();
    }

    #region 账号信息管理

    /// <summary>
    /// 加载账号信息
    /// </summary>
    private void LoadAccountInfo()
    {
        string filePath = GetAccountInfoFilePath(m_CurrentAccountId);

        if (!File.Exists(filePath))
        {
            // 创建新账号信息
            m_CurrentAccountInfo = new PlayerAccountInfo
            {
                AccountId = m_CurrentAccountId,
                CreateTime = GetCurrentTimestamp(),
                LastLoginTime = GetCurrentTimestamp(),
                SaveIdStack = new List<string>(),
            };
            SaveAccountInfo();
            DebugEx.Log("PlayerAccountDataManager", $"创建新账号信息: {m_CurrentAccountId}");
        }
        else
        {
            try
            {
                string json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                m_CurrentAccountInfo = JsonUtility.FromJson<PlayerAccountInfo>(json);
                m_CurrentAccountInfo.LastLoginTime = GetCurrentTimestamp();
                SaveAccountInfo();
                DebugEx.Log("PlayerAccountDataManager", $"加载账号信息成功: {m_CurrentAccountId}");
            }
            catch (Exception e)
            {
                DebugEx.Error("PlayerAccountDataManager", $"加载账号信息失败: {e.Message}");
                m_CurrentAccountInfo = new PlayerAccountInfo
                {
                    AccountId = m_CurrentAccountId,
                    CreateTime = GetCurrentTimestamp(),
                    LastLoginTime = GetCurrentTimestamp(),
                    SaveIdStack = new List<string>(),
                };
            }
        }
    }

    /// <summary>
    /// 保存账号信息
    /// </summary>
    private void SaveAccountInfo()
    {
        if (m_CurrentAccountInfo == null)
            return;

        try
        {
            string filePath = GetAccountInfoFilePath(m_CurrentAccountId);
            string json = JsonUtility.ToJson(m_CurrentAccountInfo, true);
            File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
        }
        catch (Exception e)
        {
            DebugEx.Error("PlayerAccountDataManager", $"保存账号信息失败: {e.Message}");
        }
    }

    /// <summary>
    /// 将存档ID移到栈顶（最近使用）
    /// </summary>
    private void MoveToStackTop(string saveId)
    {
        if (m_CurrentAccountInfo == null)
            return;

        // 移除旧位置
        m_CurrentAccountInfo.SaveIdStack.Remove(saveId);

        // 插入到最前面
        m_CurrentAccountInfo.SaveIdStack.Insert(0, saveId);

        SaveAccountInfo();
    }

    #endregion

    #region 创建新存档

    /// <summary>
    /// 创建新存档（在"新游戏"时调用）
    /// </summary>
    /// <param name="saveName">存档名称</param>
    /// <param name="summonerId">选择的召唤师ID</param>
    /// <returns>创建的存档数据</returns>
    public PlayerSaveData CreateNewSave(string saveName, int summonerId)
    {
        // 确保账号信息已加载
        if (m_CurrentAccountInfo == null)
        {
            LoadAccountInfo();
        }

        // 1. 从 PlayerInitTable 获取初始配置
        var initTable = GF.DataTable.GetDataTable<PlayerInitTable>();
        if (initTable == null)
        {
            DebugEx.Error("PlayerAccountDataManager", "PlayerInitTable 未加载");
            return null;
        }

        var initConfig = initTable.GetDataRow(1);
        if (initConfig == null)
        {
            DebugEx.Error("PlayerAccountDataManager", "找不到 PlayerInitTable 的初始配置 (Id=1)");
            return null;
        }

        // 2. 从 SummonerTable 获取召唤师配置
        var summonerTable = GF.DataTable.GetDataTable<SummonerTable>();
        if (summonerTable == null)
        {
            DebugEx.Error("PlayerAccountDataManager", "SummonerTable 未加载");
            return null;
        }

        var summonerConfig = summonerTable.GetDataRow(summonerId);
        if (summonerConfig == null)
        {
            DebugEx.Error("PlayerAccountDataManager", $"找不到召唤师配置 (Id={summonerId})");
            return null;
        }

        // 3. 生成新的存档ID
        string newSaveId = Guid.NewGuid().ToString();

        // 4. 创建 PlayerSaveData 对象
        var saveData = new PlayerSaveData
        {
            // 存档标识
            SaveId = newSaveId,
            SaveName = saveName,
            CreateTime = GetCurrentTimestamp(),
            LastPlayTime = GetCurrentTimestamp(),

            // 等级和经验
            GlobalLevel = initConfig.InitLevel,
            CurrentExp = initConfig.InitExp,

            // 召唤师系统
            CurrentSummonerId = summonerId,
            SummonerPhases = summonerId,

            // 卡牌收集
            OwnedUnitCardIds =
                initConfig.InitUnitCards != null
                    ? new List<int>(initConfig.InitUnitCards)
                    : new List<int>(),
            OwnedStrategyCardIds =
                initConfig.InitStrategyCards != null
                    ? new List<int>(initConfig.InitStrategyCards)
                    : new List<int>(),

            // 科技树
            UnlockedTechIds =
                initConfig.InitTechs != null
                    ? new List<int>(initConfig.InitTechs)
                    : new List<int>(),

            // 资源
            Gold = initConfig.InitGold,
            OriginStone = initConfig.InitOriginStone,

            // 初始化背包
            InventoryCapacity = 100, // 默认背包容量
            InventoryItems = "", // 空背包

            // 游戏进度（新存档）
            HasCompletedTutorial = false, // 新存档默认未完成教程
            CurrentSceneId = 3, // 默认进入基地场景（SceneId = 3）

            // 卡组
            CurrentDeckIndex = new List<int> { 0 },

            // 任务
            CompletedQuestIds = new List<int>(),
        };

        // 5. 初始化已解锁召唤师
        saveData.SetUnlockedSummonerIds(new List<int> { summonerId });

        // 6. 初始化背包物品
        saveData.SetInventoryItems(new List<InventoryItemSaveData>());

        // 7. 创建默认卡组
        var defaultDeck = new DeckData
        {
            DeckName = "默认卡组",
            UnitCardIds = new List<int>(saveData.OwnedUnitCardIds),
            StrategyCardIds = new List<int>(saveData.OwnedStrategyCardIds),
        };
        saveData.SetSavedDecks(new List<DeckData> { defaultDeck });

        // 8. 初始化设置和统计
        saveData.SetSettings(new PlayerSetting());
        saveData.SetStatistics(new PlayerStatistics());

        // 8.5 初始化宝物系统
        InitializeTreasureData(saveData);

        // 9. 初始化棋子解锁数据（新存档默认解锁初始棋子）
        if (ChessUnlockManager.Instance != null)
        {
            // 从配置表获取初始解锁的棋子ID列表
            var initialChessIds =
                initConfig.InitUnitCards != null
                    ? new List<int>(initConfig.InitUnitCards)
                    : new List<int>();

            ChessUnlockManager.Instance.InitializeNewSave(initialChessIds);
        }

        // 10. 保存存档
        m_CurrentSaveData = saveData;

        // 初始化图鉴管理器
        DictionaryManager.Instance.Initialize(saveData);

        SaveCurrentSave();

        // 11. 将存档ID添加到栈顶
        MoveToStackTop(newSaveId);

        DebugEx.Log("PlayerAccountDataManager", $"创建新存档成功: {saveName}, 召唤师: {summonerConfig.Name}, SaveId: {newSaveId}");

        return saveData;
    }

    /// <summary>
    /// 获取当前时间戳
    /// </summary>
    private double GetCurrentTimestamp()
    {
        return DateTime.Now.Ticks;
    }

    #endregion


    #region 存档操作

    /// <summary>
    /// 保存当前存档
    /// </summary>
    public void SaveCurrentSave()
    {
        if (m_CurrentSaveData == null)
        {
            DebugEx.Error("PlayerAccountDataManager", "当前存档为空，无法保存");
            return;
        }

        try
        {
            // 更新最后游戏时间
            m_CurrentSaveData.LastPlayTime = GetCurrentTimestamp();

            // 保存背包数据
            if (InventoryManager.Instance != null)
            {
                var inventoryData = InventoryManager.Instance.SaveInventory();
                m_CurrentSaveData.SetInventoryItems(inventoryData);
                DebugEx.Log("PlayerAccountDataManager",
                    $"背包数据已保存，物品数量:{inventoryData.Count()}"
                );
            }

            // 保存宝物数据
            SaveTreasureData(m_CurrentSaveData);

            // 序列化为JSON
            string json = JsonUtility.ToJson(m_CurrentSaveData, true);

            // 获取保存文件路径
            string filePath = GetSaveFilePath(m_CurrentAccountId, m_CurrentSaveData.SaveId);

            // 写入文件
            File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);

            DebugEx.Log("PlayerAccountDataManager", "=== 存档已保存 ===");
            DebugEx.Log("PlayerAccountDataManager", $"存档名称: {m_CurrentSaveData.SaveName}");
            DebugEx.Log("PlayerAccountDataManager", $"存档ID: {m_CurrentSaveData.SaveId}");
            DebugEx.Log("PlayerAccountDataManager", $"文件路径: {filePath}");
            DebugEx.Log("PlayerAccountDataManager", $"玩家等级: {m_CurrentSaveData.GlobalLevel}");
            DebugEx.Log("PlayerAccountDataManager", "==================");
        }
        catch (Exception e)
        {
            DebugEx.Error("PlayerAccountDataManager", $"保存存档失败: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 标记教程完成
    /// </summary>
    public void MarkTutorialCompleted()
    {
        if (m_CurrentSaveData == null)
        {
            DebugEx.Error("PlayerAccountDataManager", "当前没有加载存档");
            return;
        }

        m_CurrentSaveData.HasCompletedTutorial = true;
        SaveCurrentSave();

        DebugEx.Log("PlayerAccountDataManager", "教程已完成");
    }

    /// <summary>
    /// 加载存档（通过SaveId）
    /// </summary>
    public PlayerSaveData LoadSave(string saveId)
    {
        string filePath = GetSaveFilePath(m_CurrentAccountId, saveId);

        if (!File.Exists(filePath))
        {
            DebugEx.Warning("PlayerAccountDataManager", $"存档不存在: {saveId}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);

            if (string.IsNullOrEmpty(json))
            {
                DebugEx.Warning("PlayerAccountDataManager", $"存档文件为空: {saveId}");
                return null;
            }

            var saveData = JsonUtility.FromJson<PlayerSaveData>(json);
            saveData.LastPlayTime = GetCurrentTimestamp();
            m_CurrentSaveData = saveData;

            // 恢复宝物数据
            LoadTreasureData(saveData);

            // 上次未正常结算（异常退出），回滚到快照状态
            if (!string.IsNullOrEmpty(saveData.InGameSnapshot))
            {
                DebugEx.Warning("PlayerAccountDataManager",
                    "检测到未结算的局内快照，自动回滚账号数据");
                RestoreFromSnapshot();
            }

            // 从配置表加载运行时配置
            LoadRuntimeData(saveData);

            // 初始化图鉴管理器
            DictionaryManager.Instance.Initialize(saveData);

            // 将此存档移到栈顶
            MoveToStackTop(saveId);

            DebugEx.Log("PlayerAccountDataManager", $"加载存档成功: {saveData.SaveName} (SaveId: {saveId})");
            return saveData;
        }
        catch (Exception e)
        {
            DebugEx.Error("PlayerAccountDataManager", $"加载存档失败: {e.Message}\n{e.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// 从配置表加载运行时配置
    /// </summary>
    private void LoadRuntimeData(PlayerSaveData saveData)
    {
        var initTable = GF.DataTable.GetDataTable<PlayerInitTable>();
        if (initTable != null)
        {
        }

        // 初始化棋子解锁管理器
        if (ChessUnlockManager.Instance != null)
        {
            ChessUnlockManager.Instance.Initialize(saveData);
            DebugEx.Log("PlayerAccountDataManager", $"棋子管理器已初始化，已解锁棋子数: {saveData.OwnedUnitCardIds?.Count ?? 0}");
        }

        // 加载背包数据
        if (InventoryManager.Instance != null)
        {
            var inventoryData = saveData.GetInventoryItems();
            InventoryManager.Instance.LoadInventory(inventoryData);
            DebugEx.Log(
                "PlayerAccountDataManager",
                $"背包数据已加载，物品数量:{inventoryData.Count()}"
            );
        }
    }

    /// <summary>
    /// 删除存档
    /// </summary>
    public void DeleteSave(string saveId)
    {
        try
        {
            string filePath = GetSaveFilePath(m_CurrentAccountId, saveId);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                DebugEx.Log("PlayerAccountDataManager", $"存档已删除: {saveId}");
            }

            // 从栈中移除
            if (m_CurrentAccountInfo != null)
            {
                m_CurrentAccountInfo.SaveIdStack.Remove(saveId);
                SaveAccountInfo();
            }

            // 如果删除的是当前存档，清空当前存档
            if (m_CurrentSaveData != null && m_CurrentSaveData.SaveId == saveId)
            {
                m_CurrentSaveData = null;
            }
        }
        catch (Exception e)
        {
            DebugEx.Error("PlayerAccountDataManager", $"删除存档失败: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 获取所有存档简要信息（按栈顺序）
    /// </summary>
    public List<SaveBriefInfo> GetAllSaveBriefInfos()
    {
        if (m_CurrentAccountInfo == null)
        {
            LoadAccountInfo();
        }

        var briefInfos = new List<SaveBriefInfo>();

        foreach (var saveId in m_CurrentAccountInfo.SaveIdStack)
        {
            var briefInfo = GetSaveBriefInfo(saveId);
            if (briefInfo != null)
            {
                briefInfos.Add(briefInfo);
            }
        }

        return briefInfos;
    }

    /// <summary>
    /// 获取存档简要信息
    /// </summary>
    public SaveBriefInfo GetSaveBriefInfo(string saveId)
    {
        string filePath = GetSaveFilePath(m_CurrentAccountId, saveId);

        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            var saveData = JsonUtility.FromJson<PlayerSaveData>(json);

            return new SaveBriefInfo
            {
                SaveId = saveData.SaveId,
                SaveName = saveData.SaveName,
                GlobalLevel = saveData.GlobalLevel,
                LastPlayTime = saveData.LastPlayTime,
                CreateTime = saveData.CreateTime,
            };
        }
        catch (Exception e)
        {
            DebugEx.Error("PlayerAccountDataManager", $"获取存档信息失败: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 自动加载最近的存档
    /// </summary>
    public bool AutoLoadLastSave()
    {
        if (m_CurrentAccountInfo == null)
        {
            LoadAccountInfo();
        }

        if (m_CurrentAccountInfo.SaveIdStack.Count > 0)
        {
            string lastSaveId = m_CurrentAccountInfo.SaveIdStack[0];
            return LoadSave(lastSaveId) != null;
        }

        return false;
    }

    /// <summary>
    /// 快速保存
    /// </summary>
    public void QuickSave()
    {
        SaveCurrentSave();
    }
    #endregion


    #region 玩家数据操作

    /// <summary>
    /// 添加经验（支持连续升级）
    /// </summary>
    public void AddExp(int exp)
    {
        if (m_CurrentSaveData == null)
            return;

        m_CurrentSaveData.CurrentExp += exp;

        var levelTable = GF.DataTable.GetDataTable<PlayerDataTable>();
        if (levelTable == null)
            return;

        // 循环检查升级（防止一次大量经验跨多级）
        while (true)
        {
            var levelConfig = levelTable.GetDataRow(m_CurrentSaveData.GlobalLevel);
            // RequiredExp == 0 表示满级
            if (levelConfig == null || levelConfig.RequiredExp == 0)
                break;
            if (m_CurrentSaveData.CurrentExp < levelConfig.RequiredExp)
                break;

            m_CurrentSaveData.CurrentExp -= levelConfig.RequiredExp;
            PlayerLevelUp();
        }
    }

    /// <summary>
    /// 玩家升级（由 AddExp 内部调用，不单独触发保存）
    /// </summary>
    private void PlayerLevelUp()
    {
        if (m_CurrentSaveData == null)
            return;

        int oldLevel = m_CurrentSaveData.GlobalLevel;
        m_CurrentSaveData.GlobalLevel++;

        var levelTable = GF.DataTable.GetDataTable<PlayerDataTable>();
        if (levelTable != null)
        {
            var levelConfig = levelTable.GetDataRow(m_CurrentSaveData.GlobalLevel);
            if (levelConfig != null)
            {
                m_CurrentSaveData.InventoryCapacity = levelConfig.InventorySize;

                if (!string.IsNullOrEmpty(levelConfig.UnlockFeature))
                    HandleUnlockFeature(levelConfig.UnlockFeature);

                if (levelConfig.RewardItemId > 0)
                    AddItem(levelConfig.RewardItemId, levelConfig.RewardCount);
            }
        }

        GF.Event.Fire(this, PlayerLevelUpEventArgs.Create(oldLevel, m_CurrentSaveData.GlobalLevel));
        DebugEx.Log("PlayerAccountDataManager", $"玩家升级: {oldLevel} -> {m_CurrentSaveData.GlobalLevel}");
    }

    /// <summary>
    /// 添加物品
    /// </summary>
    public bool AddItem(int itemId, int count)
    {
        if (m_CurrentSaveData == null)
            return false;

        var items = m_CurrentSaveData.GetInventoryItems();
        var existingItem = items.Find(i => i.ItemId == itemId);

        if (existingItem != null)
        {
            existingItem.Count += count;
        }
        else
        {
            if (items.Count >= m_CurrentSaveData.InventoryCapacity)
            {
                DebugEx.Warning("PlayerAccountDataManager", "背包已满");
                return false;
            }

            items.Add(
                new InventoryItemSaveData
                {
                    ItemId = itemId,
                    Count = count,
                    ObtainTime = DateTime.Now.Ticks,
                    UniqueId = 0, // 普通物品不需要唯一ID
                    ExtraData = string.Empty, // 普通物品没有额外数据
                }
            );
        }

        m_CurrentSaveData.SetInventoryItems(items);
        return true;
    }

    /// <summary>
    /// 消耗物品
    /// </summary>
    public bool ConsumeItem(int itemId, int count)
    {
        if (m_CurrentSaveData == null)
            return false;

        var items = m_CurrentSaveData.GetInventoryItems();
        var item = items.Find(i => i.ItemId == itemId);

        if (item == null || item.Count < count)
        {
            return false;
        }

        item.Count -= count;
        if (item.Count <= 0)
        {
            items.Remove(item);
        }

        m_CurrentSaveData.SetInventoryItems(items);
        return true;
    }

    /// <summary>
    /// 添加金币
    /// </summary>
    public void AddGold(int amount)
    {
        if (m_CurrentSaveData == null)
            return;
        m_CurrentSaveData.Gold += amount;
    }

    /// <summary>
    /// 消耗金币
    /// </summary>
    public bool ConsumeGold(int amount)
    {
        if (m_CurrentSaveData == null || m_CurrentSaveData.Gold < amount)
        {
            return false;
        }
        m_CurrentSaveData.Gold -= amount;
        return true;
    }

    /// <summary>
    /// 添加魔石
    /// </summary>
    public void AddMagicalStone(int amount)
    {
        if (m_CurrentSaveData == null)
            return;
        m_CurrentSaveData.OriginStone += amount;
    }

    /// <summary>
    /// 消耗魔石
    /// </summary>
    public bool ConsumeMagicalStone(int amount)
    {
        if (m_CurrentSaveData == null || m_CurrentSaveData.OriginStone < amount)
        {
            return false;
        }
        m_CurrentSaveData.OriginStone -= amount;
        return true;
    }

    /// <summary>
    /// 添加起源石
    /// </summary>
    public void AddOriginStone(int amount)
    {
        if (m_CurrentSaveData == null)
            return;
        m_CurrentSaveData.OriginStone += amount;
        DebugEx.Log(
            "PlayerAccountDataManager",
            $"添加起源石: +{amount}, 当前: {m_CurrentSaveData.OriginStone}"
        );
    }

    /// <summary>
    /// 消耗起源石
    /// </summary>
    public bool ConsumeOriginStone(int amount)
    {
        if (m_CurrentSaveData == null)
            return false;
        if (m_CurrentSaveData.OriginStone < amount)
        {
            DebugEx.Warning("PlayerAccountDataManager",
                $"起源石不足: 需要 {amount}, 当前 {m_CurrentSaveData.OriginStone}"
            );
            return false;
        }
        m_CurrentSaveData.OriginStone -= amount;
        DebugEx.Log(
            "PlayerAccountDataManager",
            $"消耗起源石: -{amount}, 当前: {m_CurrentSaveData.OriginStone}"
        );
        return true;
    }

    /// <summary>
    /// 添加灵石
    /// </summary>
    public void AddSpiritStone(int amount)
    {
        if (m_CurrentSaveData == null)
            return;
        m_CurrentSaveData.SpiritStone += amount;
        DebugEx.Log(
            "PlayerAccountDataManager",
            $"添加灵石: +{amount}, 当前: {m_CurrentSaveData.SpiritStone}"
        );
    }

    /// <summary>
    /// 消耗灵石
    /// </summary>
    public bool ConsumeSpiritStone(int amount)
    {
        if (m_CurrentSaveData == null)
            return false;
        if (m_CurrentSaveData.SpiritStone < amount)
        {
            DebugEx.Warning("PlayerAccountDataManager",
                $"灵石不足: 需要 {amount}, 当前 {m_CurrentSaveData.SpiritStone}"
            );
            return false;
        }
        m_CurrentSaveData.SpiritStone -= amount;
        DebugEx.Log(
            "PlayerAccountDataManager",
            $"消耗灵石: -{amount}, 当前: {m_CurrentSaveData.SpiritStone}"
        );
        return true;
    }

    /// <summary>
    /// 添加圣水
    #endregion


    #region 召唤师系统

    /// <summary>
    /// 切换召唤师
    /// </summary>
    public bool ChangeSummoner(int summonerId)
    {
        if (m_CurrentSaveData == null)
            return false;

        var unlockedIds = m_CurrentSaveData.GetUnlockedSummonerIds();
        if (!unlockedIds.Contains(summonerId))
        {
            DebugEx.Warning("PlayerAccountDataManager", $"召唤师 {summonerId} 未解锁");
            return false;
        }

        m_CurrentSaveData.CurrentSummonerId = summonerId;
        m_CurrentSaveData.SummonerPhases = summonerId;
        SaveCurrentSave();
        return true;
    }

    /// <summary>
    /// 解锁召唤师
    /// </summary>
    public bool UnlockSummoner(int summonerId)
    {
        if (m_CurrentSaveData == null)
            return false;

        var unlockedIds = m_CurrentSaveData.GetUnlockedSummonerIds();
        if (unlockedIds.Contains(summonerId))
        {
            DebugEx.Warning("PlayerAccountDataManager", $"召唤师 {summonerId} 已解锁");
            return false;
        }

        var summonerTable = GF.DataTable.GetDataTable<SummonerTable>();
        if (summonerTable == null)
            return false;

        var summonerConfig = summonerTable.GetDataRow(summonerId);
        if (summonerConfig == null)
        {
            DebugEx.Error("PlayerAccountDataManager", $"找不到召唤师配置 {summonerId}");
            return false;
        }

        unlockedIds.Add(summonerId);
        m_CurrentSaveData.SetUnlockedSummonerIds(unlockedIds);

        SaveCurrentSave();
        DebugEx.Log("PlayerAccountDataManager", $"解锁召唤师: {summonerConfig.Name}");
        return true;
    }

    /// <summary>
    /// 召唤师进阶
    /// </summary>
    public bool AdvanceSummoner()
    {
        if (m_CurrentSaveData == null)
            return false;

        int currentPhaseId = m_CurrentSaveData.SummonerPhases;

        var summonerTable = GF.DataTable.GetDataTable<SummonerTable>();
        if (summonerTable == null)
            return false;

        var currentConfig = summonerTable.GetDataRow(currentPhaseId);
        if (currentConfig == null || currentConfig.NextPhaseId == 0)
        {
            DebugEx.Warning("PlayerAccountDataManager", "已达最高阶段");
            return false;
        }

        bool canAdvance = CheckAdvanceCondition(currentConfig);
        if (!canAdvance)
            return false;

        // 进阶
        m_CurrentSaveData.SummonerPhases = currentConfig.NextPhaseId;

        var unlockedIds = m_CurrentSaveData.GetUnlockedSummonerIds();
        if (!unlockedIds.Contains(currentConfig.NextPhaseId))
        {
            unlockedIds.Add(currentConfig.NextPhaseId);
            m_CurrentSaveData.SetUnlockedSummonerIds(unlockedIds);
        }

        if (m_CurrentSaveData.CurrentSummonerId == currentPhaseId)
        {
            m_CurrentSaveData.CurrentSummonerId = currentConfig.NextPhaseId;
        }

        SaveCurrentSave();
        DebugEx.Log("PlayerAccountDataManager", $"召唤师进阶成功: {currentPhaseId} -> {currentConfig.NextPhaseId}");
        return true;
    }

    /// <summary>
    /// 检查进阶条件
    /// </summary>
    private bool CheckAdvanceCondition(SummonerTable config)
    {
        switch (config.AdvanceType)
        {
            case 0:
                return false;

            case 1:
                if (m_CurrentSaveData.GlobalLevel < config.AdvanceValue)
                {
                    DebugEx.Warning("PlayerAccountDataManager", $"需要等级 {config.AdvanceValue}");
                    return false;
                }
                return true;

            case 2:
                var items = m_CurrentSaveData.GetInventoryItems();
                var item = items.Find(i => i.ItemId == config.AdvanceValue);
                if (item == null || item.Count <= 0)
                {
                    DebugEx.Warning("PlayerAccountDataManager", "缺少进阶道具");
                    return false;
                }
                ConsumeItem(config.AdvanceValue, 1);
                return true;

            case 3:
                if (!m_CurrentSaveData.CompletedQuestIds.Contains(config.AdvanceValue))
                {
                    DebugEx.Warning("PlayerAccountDataManager", "需要完成指定任务");
                    return false;
                }
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// 获取当前召唤师配置
    /// </summary>
    public SummonerTable GetCurrentSummonerConfig()
    {
        if (m_CurrentSaveData == null)
            return null;

        var summonerTable = GF.DataTable.GetDataTable<SummonerTable>();
        return summonerTable?.GetDataRow(m_CurrentSaveData.CurrentSummonerId);
    }

    #endregion

    #region 功能解锁

    /// <summary>
    /// 处理功能解锁
    /// </summary>
    private void HandleUnlockFeature(string featureName)
    {
        switch (featureName)
        {
            case "SecondCardSlot":
                DebugEx.Log("PlayerAccountDataManager", "解锁第二卡槽");
                break;

            case "InfiniteMode":
                DebugEx.Log("PlayerAccountDataManager", "解锁无限模式");
                break;

            default:
                DebugEx.Log("PlayerAccountDataManager", $"解锁功能: {featureName}");
                break;
        }
    }

    #endregion

    #region 局内快照

    /// <summary>
    /// 进入局内前创建快照，保存当前账号数据到 InGameSnapshot 字段
    /// </summary>
    public void CreateInGameSnapshot()
    {
        if (m_CurrentSaveData == null)
            return;

        // 序列化当前关键数据为快照（排除快照字段本身，避免递归）
        var snapshot = new PlayerSaveSnapshot
        {
            GlobalLevel = m_CurrentSaveData.GlobalLevel,
            CurrentExp  = m_CurrentSaveData.CurrentExp,
            Gold        = m_CurrentSaveData.Gold,
            OriginStone = m_CurrentSaveData.OriginStone,
            SpiritStone = m_CurrentSaveData.SpiritStone,
            InventoryItems    = m_CurrentSaveData.InventoryItems,
            InventoryCapacity = m_CurrentSaveData.InventoryCapacity,
            CompletedQuestIds = m_CurrentSaveData.CompletedQuestIds != null
                ? new List<int>(m_CurrentSaveData.CompletedQuestIds)
                : new List<int>(),
        };

        m_CurrentSaveData.InGameSnapshot = JsonUtility.ToJson(snapshot);
        SaveCurrentSave();

        DebugEx.Log("PlayerAccountDataManager",
            $"已创建局内快照: Lv={snapshot.GlobalLevel}, Exp={snapshot.CurrentExp}");
    }

    /// <summary>
    /// 正常结算后清除快照（传送/死亡回基地时调用）
    /// </summary>
    public void ClearInGameSnapshot()
    {
        if (m_CurrentSaveData == null)
            return;

        m_CurrentSaveData.InGameSnapshot = null;
        DebugEx.Log("PlayerAccountDataManager", "局内快照已清除");
    }

    /// <summary>
    /// 检查是否存在未清除的快照（说明上次未正常结算）
    /// </summary>
    public bool HasInGameSnapshot()
    {
        return m_CurrentSaveData != null && !string.IsNullOrEmpty(m_CurrentSaveData.InGameSnapshot);
    }

    /// <summary>
    /// 获取局内快照数据（用于结算计算）
    /// </summary>
    public PlayerSaveSnapshot GetInGameSnapshotData()
    {
        if (m_CurrentSaveData == null || string.IsNullOrEmpty(m_CurrentSaveData.InGameSnapshot))
            return null;
        try
        {
            return JsonUtility.FromJson<PlayerSaveSnapshot>(m_CurrentSaveData.InGameSnapshot);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 将账号数据回滚到快照状态（异常退出后重新进入游戏时调用）
    /// </summary>
    public void RestoreFromSnapshot()
    {
        if (m_CurrentSaveData == null || string.IsNullOrEmpty(m_CurrentSaveData.InGameSnapshot))
            return;

        try
        {
            var snapshot = JsonUtility.FromJson<PlayerSaveSnapshot>(m_CurrentSaveData.InGameSnapshot);

            m_CurrentSaveData.GlobalLevel        = snapshot.GlobalLevel;
            m_CurrentSaveData.CurrentExp         = snapshot.CurrentExp;
            m_CurrentSaveData.Gold               = snapshot.Gold;
            m_CurrentSaveData.OriginStone        = snapshot.OriginStone;
            m_CurrentSaveData.SpiritStone        = snapshot.SpiritStone;
            m_CurrentSaveData.InventoryItems     = snapshot.InventoryItems;
            m_CurrentSaveData.InventoryCapacity  = snapshot.InventoryCapacity;
            m_CurrentSaveData.CompletedQuestIds  = snapshot.CompletedQuestIds ?? new List<int>();
            m_CurrentSaveData.InGameSnapshot     = null;

            SaveCurrentSave();

            DebugEx.Log("PlayerAccountDataManager",
                $"已从快照恢复: Lv={snapshot.GlobalLevel}, Exp={snapshot.CurrentExp}");
        }
        catch (System.Exception e)
        {
            DebugEx.Error("PlayerAccountDataManager", $"快照恢复失败: {e.Message}");
        }
    }

    #endregion

    #region 宝物系统

    /// <summary>
    /// 装备宝物给棋子
    /// </summary>
    /// <summary>
    /// 获取宝物（添加到存档，自动生成 InstanceId）
    /// 用于掉落、宝箱、任务奖励等获取宝物的场景
    /// </summary>
    public TreasureInstanceData ObtainTreasure(TreasureItem treasureItem)
    {
        if (treasureItem == null || m_CurrentSaveData == null)
        {
            DebugEx.Error(nameof(PlayerAccountDataManager), "获取宝物失败：物品或存档为空");
            return null;
        }

        // 生成唯一 InstanceId（使用时间戳 + 随机数保证唯一性）
        int instanceId = GenerateUniqueInstanceId();

        // 转换为持久化数据（自动调用 GetAffixDataForPersistence，保存词条）
        var instanceData = ItemManager.Instance.CreateTreasureInstanceData(treasureItem, instanceId);
        if (instanceData == null)
        {
            DebugEx.Error(nameof(PlayerAccountDataManager), $"创建宝物实例数据失败: {treasureItem.Name}");
            return null;
        }

        // 添加到存档
        m_CurrentSaveData.Treasures.Add(instanceData);

        DebugEx.Success(nameof(PlayerAccountDataManager),
            $"✅ 宝物已获取: {treasureItem.Name} (InstanceId:{instanceId}, 词条数:{instanceData.Affixes.Count})");

        return instanceData;
    }

    /// <summary>
    /// 生成唯一的 InstanceId
    /// </summary>
    private int GenerateUniqueInstanceId()
    {
        // 使用时间戳 + 随机数确保唯一性
        long timestamp = System.DateTime.Now.Ticks;
        int randomPart = UnityEngine.Random.Range(1000, 9999);
        return (int)((timestamp / 100000) % int.MaxValue) ^ randomPart;
    }

    public void EquipTreasure(int instanceId, int chessId)
    {
        var saveData = CurrentSaveData;
        if (saveData == null) return;

        var treasure = saveData.Treasures.FirstOrDefault(t => t.InstanceId == instanceId);
        if (treasure == null || treasure.Location == TreasureLocation.Warehouse)
            return; // 只能装备背包中的宝物

        if (!saveData.ChessTreasureSlots.ContainsKey(chessId))
            saveData.ChessTreasureSlots[chessId] = new();

        var slots = saveData.ChessTreasureSlots[chessId];

        // 最多装备 3 个且不重复
        if (slots.Count < 3 && !slots.Contains(instanceId))
        {
            slots.Add(instanceId);
            treasure.EquippedChessId = chessId;
        }
    }

    /// <summary>
    /// 卸载宝物（保留在原位置，只取消装备）
    /// </summary>
    public void UnequipTreasure(int instanceId)
    {
        var saveData = CurrentSaveData;
        var treasure = saveData?.Treasures.FirstOrDefault(t => t.InstanceId == instanceId);

        if (treasure == null || treasure.EquippedChessId == 0)
            return;

        int chessId = treasure.EquippedChessId;

        // 从装备槽中移除
        if (saveData.ChessTreasureSlots.TryGetValue(chessId, out var slots))
            slots.Remove(instanceId);

        treasure.EquippedChessId = 0;  // 取消装备标记
    }

    /// <summary>
    /// 获取棋子的装备列表
    /// </summary>
    public List<TreasureInstanceData> GetChessEquipments(int chessId)
    {
        var saveData = CurrentSaveData;
        if (saveData == null) return new();

        return saveData.Treasures
            .Where(t => t.EquippedChessId == chessId)
            .ToList();
    }

    /// <summary>
    /// 获取背包中的宝物（包括已装备的）
    /// </summary>
    public List<TreasureInstanceData> GetInventoryTreasures()
    {
        var saveData = CurrentSaveData;
        if (saveData == null) return new();

        return saveData.Treasures
            .Where(t => t.Location == TreasureLocation.Inventory)
            .ToList();
    }

    /// <summary>
    /// 获取仓库中的宝物
    /// </summary>
    public List<TreasureInstanceData> GetWarehouseTreasures()
    {
        var saveData = CurrentSaveData;
        if (saveData == null) return new();

        return saveData.Treasures
            .Where(t => t.Location == TreasureLocation.Warehouse)
            .ToList();
    }

    /// <summary>
    /// 将宝物从背包移到仓库
    /// </summary>
    public void MoveToWarehouse(int instanceId)
    {
        var saveData = CurrentSaveData;
        var treasure = saveData?.Treasures.FirstOrDefault(t => t.InstanceId == instanceId);

        if (treasure == null) return;

        // 先卸装
        if (treasure.EquippedChessId != 0)
            UnequipTreasure(instanceId);

        // 再移动
        treasure.Location = TreasureLocation.Warehouse;
    }

    /// <summary>
    /// 将宝物从仓库移到背包
    /// </summary>
    public void MoveToInventory(int instanceId)
    {
        var saveData = CurrentSaveData;
        var treasure = saveData?.Treasures.FirstOrDefault(t => t.InstanceId == instanceId);

        if (treasure == null) return;

        treasure.Location = TreasureLocation.Inventory;
    }

    /// <summary>
    /// 获取宝物实例
    /// </summary>
    public TreasureInstanceData GetTreasureInstanceById(int instanceId)
    {
        var saveData = CurrentSaveData;
        if (saveData == null) return null;

        return saveData.Treasures.FirstOrDefault(t => t.InstanceId == instanceId);
    }

    /// <summary>
    /// 序列化宝物数据到 JSON 字符串
    /// </summary>
    private void SaveTreasureData(PlayerSaveData saveData)
    {
        if (saveData == null) return;

        // 1. 序列化宝物列表
        if (saveData.Treasures != null && saveData.Treasures.Count > 0)
        {
            try
            {
                var treasureList = new SerializedTreasureList { Items = saveData.Treasures };
                saveData.TreasureData = JsonUtility.ToJson(treasureList, true);
            }
            catch
            {
                DebugEx.Error(nameof(PlayerAccountDataManager), "序列化宝物数据失败");
                saveData.TreasureData = "";
            }
        }
        else
        {
            saveData.TreasureData = "";
        }

        // 2. 序列化宝物槽数据
        if (saveData.ChessTreasureSlots != null && saveData.ChessTreasureSlots.Count > 0)
        {
            try
            {
                var equipmentSlots = new SerializedChessTreasureSlots();
                foreach (var kvp in saveData.ChessTreasureSlots)
                {
                    equipmentSlots.Slots.Add(new SerializedChessTreasureSlot
                    {
                        ChessId = kvp.Key,
                        InstanceIds = kvp.Value
                    });
                }
                saveData.ChessTreasureSlotsData = JsonUtility.ToJson(equipmentSlots, true);
            }
            catch
            {
                DebugEx.Error(nameof(PlayerAccountDataManager), "序列化宝物槽数据失败");
                saveData.ChessTreasureSlotsData = "";
            }
        }
        else
        {
            saveData.ChessTreasureSlotsData = "";
        }
    }

    /// <summary>
    /// 从 JSON 字符串反序列化宝物数据
    /// </summary>
    private void LoadTreasureData(PlayerSaveData saveData)
    {
        if (saveData == null) return;

        // 1. 恢复宝物列表
        if (!string.IsNullOrEmpty(saveData.TreasureData))
        {
            try
            {
                var treasureList = JsonUtility.FromJson<SerializedTreasureList>(saveData.TreasureData);
                saveData.Treasures = treasureList.Items ?? new List<TreasureInstanceData>();
            }
            catch
            {
                DebugEx.Error(nameof(PlayerAccountDataManager), "加载宝物数据失败，初始化为空列表");
                saveData.Treasures = new List<TreasureInstanceData>();
            }
        }
        else
        {
            saveData.Treasures = new List<TreasureInstanceData>();
        }

        // 2. 恢复宝物槽数据
        if (!string.IsNullOrEmpty(saveData.ChessTreasureSlotsData))
        {
            try
            {
                var equipmentSlots = JsonUtility.FromJson<SerializedChessTreasureSlots>(saveData.ChessTreasureSlotsData);
                saveData.ChessTreasureSlots = new Dictionary<int, List<int>>();

                foreach (var slot in equipmentSlots.Slots)
                {
                    saveData.ChessTreasureSlots[slot.ChessId] = slot.InstanceIds ?? new List<int>();
                }
            }
            catch
            {
                DebugEx.Error(nameof(PlayerAccountDataManager), "加载宝物槽数据失败，初始化为空字典");
                saveData.ChessTreasureSlots = new Dictionary<int, List<int>>();
            }
        }
        else
        {
            saveData.ChessTreasureSlots = new Dictionary<int, List<int>>();
        }
    }

    /// <summary>
    /// 初始化新存档的宝物数据
    /// </summary>
    private void InitializeTreasureData(PlayerSaveData saveData)
    {
        if (saveData == null) return;

        // 初始化空列表
        saveData.Treasures = new List<TreasureInstanceData>();
        saveData.ChessTreasureSlots = new Dictionary<int, List<int>>();
    }

    #endregion
}
