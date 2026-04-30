namespace AAAGame.Audio
{
    /// <summary>
    /// 音效 ID 常量定义（对应 AudioClipTable 中的 ID 1-83）
    /// </summary>
    public static class AudioClipIds
    {
        // ==================== BGM (ID 1-23) ====================

        public const int BGM_StartGame = 1;
        public const int BGM_BaseRoom = 2;
        public const int BGM_WorldScene = 3;
        public const int BGM_TutorialScene = 4;
        public const int BGM_Tiangong = 5;
        public const int BGM_Herheim = 6;
        public const int BGM_Takamahara = 7;
        public const int BGM_Olympus = 8;
        public const int BGM_Babylon = 9;
        public const int BGM_Avalon = 10;
        public const int BGM_Abyss = 11;
        public const int BGM_CombatNormal = 12;
        public const int BGM_CombatElite = 13;
        public const int BGM_CombatBoss = 14;
        public const int BGM_Settlement = 15;
        public const int BGM_BattlePreset = 16;
        public const int BGM_Inventory = 17;
        public const int BGM_Shop = 18;
        public const int BGM_Upgrade = 19;

        // ==================== SFX (ID 20-83) ====================

        // UI 音效 (20-29)
        public const int SFX_ButtonClick = 20;
        public const int SFX_UIOpen = 21;
        public const int SFX_UIClose = 22;
        public const int SFX_ButtonHover = 23;
        public const int SFX_TabSwitch = 24;
        public const int SFX_Confirm = 25;
        public const int SFX_Cancel = 26;
        public const int SFX_Error = 27;
        public const int SFX_Success = 28;
        public const int SFX_Unlock = 29;

        // UI 进阶音效 (30-35)
        public const int SFX_Notification = 30;
        public const int SFX_CurrencyGain = 31;
        public const int SFX_ChessSelect = 32;
        public const int SFX_MoveStart = 33;
        public const int SFX_MoveStop = 34;
        public const int SFX_AttackCharge = 35;

        // 战斗反馈音 (36-50)
        public const int SFX_AttackRelease = 36;
        public const int SFX_AttackWhoosh = 37;
        public const int SFX_SkillCast = 38;
        public const int SFX_BuffApply = 39;
        public const int SFX_DebuffApply = 40;
        public const int SFX_HitLight = 41;
        public const int SFX_HitNormal = 42;
        public const int SFX_HitHeavy = 43;
        public const int SFX_HitCritical = 44;
        public const int SFX_BlockDodge = 45;
        public const int SFX_Heal = 46;
        public const int SFX_ShieldGain = 47;
        public const int SFX_Death = 48;
        public const int SFX_TurnStart = 49;
        public const int SFX_TurnEnd = 50;

        // 战斗环境 (51-56)
        public const int SFX_CombatStart = 51;
        public const int SFX_BossAppear = 52;
        public const int SFX_BossSkillWarning = 53;
        public const int SFX_CombatVictory = 54;
        public const int SFX_CombatDefeat = 55;
        public const int SFX_Footstep = 56;

        // 探索交互音 (57-63)
        public const int SFX_ItemPickup = 57;
        public const int SFX_ChestOpen = 58;
        public const int SFX_DoorOpen = 59;
        public const int SFX_DoorClose = 60;
        public const int SFX_NPCDialogue = 61;
        public const int SFX_QuestAccept = 62;
        public const int SFX_QuestComplete = 63;

        // 环境音 (64-70)
        public const int SFX_AmbientWind = 64;
        public const int SFX_AmbientThunder = 65;
        public const int SFX_AmbientWater = 66;
        public const int SFX_AmbientFire = 67;
        public const int SFX_AmbientChill = 68;
        public const int SFX_MysteryWhisper = 69;

        // 传送与切换 (70-73)
        public const int SFX_TeleportIn = 70;
        public const int SFX_TeleportOut = 71;
        public const int SFX_SceneLoad = 72;
        public const int SFX_TeleportReady = 73;

        // 成就与奖励 (74-83)
        public const int SFX_AchievementUnlock = 74;
        public const int SFX_LevelUp = 75;
        public const int SFX_RankUp = 76;
        public const int SFX_BonusReward = 77;
        public const int SFX_LegendaryItem = 78;
        public const int SFX_CoinPickup = 79;
        public const int SFX_GemPickup = 80;
        public const int SFX_CoinSpend = 81;
        public const int SFX_GemSpend = 82;
        public const int SFX_Transaction = 83;

        // ==================== 默认 ID ====================
        public const int DEFAULT_INVALID_ID = 0;
    }
}
