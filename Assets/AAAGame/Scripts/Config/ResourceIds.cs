/// <summary>
/// 资源配置ID静态类 - 包含所有游戏资源配置表ID
/// 使用方式: ResourceIds.BACKGROUND_MAIN
/// </summary>
public static class ResourceIds
{
    #region UI背景图片 (1000-1099)
    public const int MENU_BACKGROUND = 1001;
    public const int MENU_NAME = 1002;
    public const int MENU_NAME_EN = 1003;
    public const int MENU_YUN = 1004;
    #endregion

    #region 物品图标 (1100-1199)
    public const int ICON_GOLD = 1101;
    public const int ICON_MAGICAL_STONE = 1102;
    public const int ICON_HOLY_WATER = 1103;

    /// <summary>战斗中棋子放置预览图片</summary>
    public const int ICON_PLACEMENT_PREVIEW = 1104;
    #endregion

    #region 卡牌图标 (1200-1299)
    public const int ICON_CARD_HOLY_LIGHT = 1201;
    #endregion

    #region 棋子图标 (1300-1399)
    public const int ICON_CHESS_HOUYI = 1301;

    // 注意：缺失嫦娥棋子图标
    public const int ICON_CHESS_CHANGE = 1304;
    #endregion

    #region Buff图标 (1400-1499)
    public const int ICON_BUFF_Fire = 1401;

    // 注意：缺失冰霜Buff图标
    public const int ICON_BUFF_FROST = 1402;
    public const int ICON_BUFF_MELT = 1403;
    public const int ICON_BUFF_DIVINE_POWER = 1404;
    public const int ICON_BUFF_SUNSET_BOW = 1405;
    public const int ICON_BUFF_NINE_HEAVEN_ICE = 1406;
    #endregion

    #region 技能图标 (1500-1599)
    public const int ICON_SKILL_DASH = 1501;
    public const int ICON_SKILL_HEAL = 1502;
    public const int ICON_SKILL_BOMB = 1503;
    #endregion

    #region 装备图标 (1600-1699)
    public const int ICON_EQUIP_SUNSET_BOW = 1601;
    #endregion

    #region 召唤师技能图标 (1700-1799)
    // 狂战士技能图标
    public const int ICON_SKILL_SUMMONER_BERSERKER_PASSIVE = 1701; // 狂怒之心（被动）
    public const int ICON_SKILL_SUMMONER_BERSERKER_ACTIVE = 1702; // 战意激昂（主动）
    public const int ICON_SKILL_SUMMONER_ROYAL_COMMAND = 1703; // 王者号令（3阶，路线一）
    public const int ICON_SKILL_SUMMONER_IRON_TORRENT = 1704; // 钢铁洪流（4阶，路线一）
    public const int ICON_SKILL_SUMMONER_CALAMITY_DESCENDS = 1705; // 天灾降临（5阶，路线一）
    public const int ICON_SKILL_SUMMONER_ANNIHILATION_SLASH = 1706; // 寂灭斩（3阶，路线二）
    public const int ICON_SKILL_SUMMONER_LONE_SHADOW = 1707; // 孤影（4阶，路线二）
    public const int ICON_SKILL_SUMMONER_JUDGMENT_MOMENT = 1708; // 裁决之刻（5阶，路线二）

    // 术士技能图标
    public const int ICON_SKILL_SUMMONER_SHADOW_CURSE_BODY = 1711; // 暗影咒体（被动）
    public const int ICON_SKILL_SUMMONER_LIFE_DRAIN = 1712; // 生命虹吸（主动）
    #endregion

    // 注意：以下是棋子技能图标配置
    #region 棋子技能图标 (1800-1899)
    public const int ICON_SKILL_CHESS_SUNSET_BOW = 1801;
    public const int ICON_SKILL_CHESS_HOUYI_NORMAL_ATTACK = 1802;
    public const int ICON_SKILL_CHESS_DIVINE_POWER = 1803;
    public const int ICON_SKILL_CHESS_SUN_FALL = 1804;
    public const int ICON_SKILL_CHESS_NINE_HEAVEN_ICE = 1805;
    public const int ICON_SKILL_CHESS_CHANGE_NORMAL_ATTACK = 1806;
    public const int ICON_SKILL_CHESS_MOON_WHEEL = 1807;
    public const int ICON_SKILL_CHESS_MOON_FALL = 1808;
    #endregion

    #region UI预制体 (2000-2099)
    public const int PREFAB_PLAYER_INFO_ITEM = 2001;
    public const int PREFAB_ITEMS_INFO_ITEM = 2002;
    public const int PREFAB_TIME_INFO_ITEM = 2003;
    public const int PREFAB_SUMMON_CHESS_STATE_UI = 2004;
    #endregion

    #region 角色/NPC预制体 (2100-2199)
    public const int PREFAB_CHAR_BERSERKER = 2101;
    public const int PREFAB_CHAR_WARLOCK = 2102;
    public const int PREFAB_CHAR_CHAOS = 2103;
    public const int PREFAB_CHAR_DRUID = 2104;
    #endregion

    #region 道具预制体 (2200-2299)
    public const int PREFAB_ITEM_DASH = 2201;
    #endregion

    #region 神话角色预制体 (2300-2399)
    public const int PREFAB_CHAR_HOUYI = 2301;

    // 注意：缺失嫦娥棋子预制体
    public const int PREFAB_CHAR_CHANGE = 2304;
    #endregion

    // 注意：以下是战斗场景预制体配置
    #region 战斗场景预制体 (2900-2999)
    public const int PREFAB_BATTLE_ARENA = 2901;
    #endregion

    #region ScriptableObject资源 (6000-6999)
    /// <summary>
    /// 技能参数注册表
    /// </summary>
    public const int SO_SKILL_PARAM_REGISTRY = 6001;
    #endregion
}
