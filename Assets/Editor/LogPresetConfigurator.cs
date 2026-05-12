using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 日志预设配置器 - 手工精准定义每个模块的预设
/// 每个预设只包含该模块的核心脚本，避免无关日志污染
/// </summary>
public class LogPresetConfigurator
{
    [MenuItem("工具/日志工具/生成精准预设配置")]
    public static void GeneratePresetsWithManualConfig()
    {
        // 先扫描所有节点
        var allScripts = GetAllDebugExScripts();
        if (allScripts.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "未找到使用 DebugEx 的脚本", "确定");
            return;
        }

        // 获取所有节点名称集合，用于验证预设中的脚本是否存在
        var scriptSet = new HashSet<string>(allScripts);

        // 生成预设
        CreatePreset("全部启用", allScripts.ToDictionary(s => s, _ => true));
        CreatePreset("全部禁用", allScripts.ToDictionary(s => s, _ => false));

        // ========== 战斗系统 ==========
        var combatScripts = new[]
        {
            "CombatManager", "CombatState", "CombatUI",
            "CombatTriggerManager", "CombatPreparationState", "CombatPreparationUI",
            "BattleChessManager", "ChessPlacementManager", "ChessDeploymentTracker",
            "CombatEntityTracker", "EffectExecutor", "DamageFloatingTextManager",
            "CombatEscapeSystem", "BattleArenaManager", "EnterCombatTip",
            "EnemyChessDetailManager", "EnemyChessDataManager", "EnemyChessCombatState",
            "SummonerCombatProxy", "SummonerDeathHandler",
            "CombatOpportunityDetector", "CombatTestBootstrapper", "CombatTestController",
            // 难度调整相关（根据难度等级调整敌人棋子属性）
            "DifficultyCalculator", "EnemySpawnManager", "ChessAttribute"
        };
        CreatePreset("战斗系统", BuildPreset(allScripts, combatScripts, scriptSet));

        // ========== 棋子战斗与AI ==========
        var chessScripts = new[]
        {
            "ChessEntity", "ChessCombatController", "ChessFactory",
            "ChessDataManager", "ChessLifecycleHandler", "ChessEquipmentManager",
            "ChessAnimator", "ChessAttribute", "ChessEXPManager",
            "ChessAIBase", "FSMMeleeAI", "FSMRangedAI", "DefaultSkillReleaseStrategy",
            "ChangeNormalAttack", "ChangeSkill1", "ChangeUltimate", "ChangePassive", "ChangeMagicCircle",
            "HouyiNormalAttack", "HouyiSkill1", "HouyiUltimate", "HouyiPassive",
            "EvilSpiritNormalAttack", "EvilSpiritSkill1", "EvilSpiritUltimate", "EvilSpiritPassive", "EvilSpiritMagicCircle",
            "EvilSoulNormalAttack", "EvilSoulSkill1", "EvilSoulUltimate", "EvilSoulPassive",
            "DarkYangyuanNormalAttack", "DarkYangyuanSkill1", "DarkYangyuanSkill2", "DarkYangyuanUltimate", "DarkYangyuanPassive",
            "DarkWolfdogManager", "DarkYangyuanPhaseConfig",
            "ChessNormalAttackBase", "ChessSkillBase",
            "SummonChessManager", "GlobalChessManager", "ChessUnlockManager",
            "ChessStateUIWorldManager", "TargetHighlightManager", "ChessTestInput",
            // 索敌系统（AI目标选择）
            "DefaultTargetSearchStrategy", "TargetSearchConfig", "EnemyInfoCache", "CombatEntityTracker"
        };
        CreatePreset("棋子系统", BuildPreset(allScripts, chessScripts, scriptSet));

        // ========== Buff系统 ==========
        var buffScripts = new[]
        {
            "BuffManager", "BuffFactory", "BuffApplyHelper",
            "BleedBuff", "BurnBuff", "ConfusedBuff", "CorrosionBuff", "FearBuff", "FrostBuff",
            "LifestealBuff", "MaxHpBuff", "MeltBuff", "PoisonBuff", "ReflectDamageBuff",
            "StatModBuff", "StunBuff", "TauntBuff", "DarkFuryBuff", "BerserkerRageBuff",
            "ShieldRegenBuff", "SilenceBuff",
            "BuffTestCompilationFix", "BuffTestExample", "BuffTestIntegration", "BuffTestTool", "BuffTestUIManager"
        };
        CreatePreset("Buff系统", BuildPreset(allScripts, buffScripts, scriptSet));

        // ========== 效果系统 ==========
        var effectScripts = new[]
        {
            "GameEffectService", "PassiveEffect", "TriggerEffect", "InstantEffect",
            "SpecialEffectManager", "SpecialEffectData",
            "CardEffectExecutor", "CardEffectHelper",
            "GenericCardEffect", "LifeDrainCardEffect", "ResurrectionCardEffect",
            "TargetSelectors", "AffixEffect", "AffixGenerator", "ItemEffectExecutor", "ItemEffectFactory"
        };
        CreatePreset("效果系统", BuildPreset(allScripts, effectScripts, scriptSet));

        // ========== 卡牌系统 ==========
        var cardScripts = new[]
        {
            "CardManager", "CardData", "CardPreviewDisplay", "CardPreviewDisplayShader",
            "CardRangePreview", "CardSlotContainer", "CardSlotItem", "CardSlotItemEventDispatcher",
            "CardSlotItemPool", "CardPresetItem", "BattlePresetManager", "BattlePresetUI"
        };
        CreatePreset("卡牌系统", BuildPreset(allScripts, cardScripts, scriptSet));

        // ========== 物品背包系统 ==========
        var itemScripts = new[]
        {
            "ItemManager", "ItemBase", "TreasureItem", "ConsumableItem", "EquipmentItem",
            "ItemStack", "InventoryManager", "InventorySlot", "InventoryClickHandler", "InventoryDragHandler",
            "InventoryUI", "InventoryItemUI", "InventorySlotUI", "InventorySlotContainerImpl",
            "WarehouseManager", "WarehouseUI", "WarehouseSlotContainerImpl",
            "DictionaryManager", "DictionariesUI", "DictionaryItem", "DictionarySlot",
            "CurrencyItem", "QuestItem", "VirtualItem",
            "TreasureBoxUI", "TreasureBoxSlotContainerImpl", "TreasureChestInteractable",
            "WorldItemPickup", "MapItemUI"
        };
        CreatePreset("物品背包", BuildPreset(allScripts, itemScripts, scriptSet));

        // ========== 探索系统 ==========
        var exploreScripts = new[]
        {
            "SceneSpawnManager", "SpawnConfigParser", "EnemySpawnManager", "EnemyFormationManager",
            "EnemyEntity", "EnemyEntityManager", "EnemyEntityAI", "EnemyAnimator",
            "EnemyIdleState", "EnemyPatrolState", "EnemyAlertState", "EnemyChaseState", "EnemyCombatState",
            "EnemyAlertUIManager", "EnemyMask", "EnemyInfoCache",
            "EnemyGroupManager", "EnemyRewardSystem",
            "ExploreAITestBootstrapper", "EnemyTestController",
            "ExplorationState", "InteractionDetector", "PlayerInteraction",
            "TreasureChestInteractable", "PostCombatStealth",
            "DifficultyCalculator"
        };
        CreatePreset("探索系统", BuildPreset(allScripts, exploreScripts, scriptSet));

        // ========== 敌人AI详细 ==========
        var enemyAIScripts = new[]
        {
            "EnemyEntityAI", "EnemyIdleState", "EnemyPatrolState", "EnemyAlertState", "EnemyChaseState", "EnemyCombatState",
            "EnemyRestState", "EnemyAlertedByBroadcastState",
            "FSMMeleeAI", "FSMRangedAI", "DummyAI", "DefaultTargetSearchStrategy"
        };
        CreatePreset("敌人AI", BuildPreset(allScripts, enemyAIScripts, scriptSet));

        // ========== 索敌系统（AI目标选择调试） ==========
        var targetSearchScripts = new[]
        {
            "DefaultTargetSearchStrategy", "TargetSearchConfig", "EnemyInfoCache", "CombatEntityTracker",
            "ChessAIBase", "FSMMeleeAI", "FSMRangedAI", "DefaultSkillReleaseStrategy"
        };
        CreatePreset("索敌系统", BuildPreset(allScripts, targetSearchScripts, scriptSet));

        // ========== UI系统 ==========
        var uiScripts = new[]
        {
            "CombatUI", "CombatPreparationUI", "EndCombatUI",
            "InventoryUI", "InventoryItemUI", "InventorySlotUI",
            "BattlePresetUI", "DetailInfoUI", "DictionariesUI",
            "SummonChessStateUI", "ChessItemUI", "ChessStateUIWorldManager",
            "CardSlotItem", "BuffItem", "AwardItemUI",
            "TreasureBoxUI", "MapItemUI", "WarehouseUI",
            "GamePlayInfoUI", "GameUIForm",
            "EnemyAlertUIManager", "FloatingBoxTip", "EnterCombatTip",
            "TargetHighlightManager", "DamagePopupItem"
        };
        CreatePreset("UI系统", BuildPreset(allScripts, uiScripts, scriptSet));

        // ========== 音频系统 ==========
        var audioScripts = new[]
        {
            "AudioManager", "BGMManager", "SFXManager", "VolumeManager",
            "AudioEventListener", "AudioResourceManager"
        };
        CreatePreset("音频系统", BuildPreset(allScripts, audioScripts, scriptSet));

        // ========== 框架系统（仅核心）==========
        var frameworkScripts = new[]
        {
            "GameProcedure", "PreloadProcedure", "GameStateManager",
            "CombatState", "ExplorationState", "InGameState", "OutOfGameState", "MenuState",
            "PlayerAccountDataManager", "PlayerCharacterManager", "PlayerExpManager",
            "PlayerRuntimeDataManager", "SummonerRuntimeDataManager",
            "GameTestManager", "GameTestWindow", "CombatTestBootstrapper"
        };
        CreatePreset("框架系统", BuildPreset(allScripts, frameworkScripts, scriptSet));

        // ========== 高性能调试用 ==========
        var minimumScripts = new[]
        {
            "CombatManager", "SummonChessManager", "BuffManager", "ItemManager", "InventoryManager"
        };
        CreatePreset("最小化", BuildPreset(allScripts, minimumScripts, scriptSet));

        EditorUtility.DisplayDialog("生成完成",
            $"已为 {allScripts.Count} 个日志节点生成 14 个精准预设\n\n" +
            "预设列表：\n" +
            "• 全部启用 / 全部禁用\n" +
            "• 战斗系统 / 棋子系统 / Buff系统\n" +
            "• 效果系统 / 卡牌系统 / 物品背包\n" +
            "• 探索系统 / 敌人AI\n" +
            "• UI系统 / 音频系统 / 框架系统\n" +
            "• 最小化",
            "确定");
    }

    private static List<string> GetAllDebugExScripts()
    {
        var scriptNames = new HashSet<string>();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Script");

        foreach (var guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (!assetPath.EndsWith(".cs")) continue;

            try
            {
                string fileContent = System.IO.File.ReadAllText(assetPath);
                if (!fileContent.Contains("DebugEx.")) continue;
            }
            catch { continue; }

            var scriptAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
            if (scriptAsset?.GetClass() != null)
                scriptNames.Add(scriptAsset.GetClass().Name);
        }

        return scriptNames.OrderBy(s => s).ToList();
    }

    private static Dictionary<string, bool> BuildPreset(List<string> allScripts, string[] enabledScripts, HashSet<string> scriptSet)
    {
        var preset = new Dictionary<string, bool>();
        var enabledSet = new HashSet<string>(enabledScripts);

        foreach (var script in allScripts)
        {
            preset[script] = enabledSet.Contains(script);
        }
        return preset;
    }

    private static void CreatePreset(string presetName, Dictionary<string, bool> states)
    {
        LogConfigManager.SavePreset(presetName, states);
        int enabledCount = states.Count(x => x.Value);
        Debug.Log($"[LogPresetConfigurator] 预设 \"{presetName}\" 已生成 ({enabledCount}/{states.Count} 启用)");
    }
}
