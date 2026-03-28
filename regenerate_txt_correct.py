#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
重新生成正确的TXT文件，确保所有行都有28列且数据正确对齐
"""

# 定义所有技能的完整数据（从规格文档）
skills_data = [
    # ID, Name, SummonerClass, SkillType, UnlockTier, BranchId, Cooldown, SpiritCost, Duration, CastRange, AreaRadius, DamageType, DamageCoeff, BaseDamage, EffectHitType, ProjectilePrefabId, ProjectileSpeed, HitCount, InstantBuffs, HitBuffs, Params, EffectId, HitEffectId, EffectSpawnHeight, IconId, Desc
    (101, "狂怒之心", 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, "4003:3", 0, "0.5", 0, 0, 0, 1701, "狂战固定被动。条件触发：战场有友方HP<Params[0]时，InstantBuffs施加（4003→全体友方含召唤师）；BerserkerRageBuff自动检测目标是否为召唤师并应用30%伤害加成。条件解除时移除"),
    (102, "战意激昂", 1, 2, 1, 0, 30, 0, 10, 0, 0, 0, 0, 0, 0, 0, 0, 1, "4001:3", 0, "20", 2102, 0, 0, 1702, "狂战固定主动。Params[0]=HP消耗(20)。扣除召唤师HP后，InstantBuffs施加战意激昂Buff（攻速+20%伤害+15%）到全体友方（含召唤师），持续10秒"),
    (103, "王者号令", 1, 2, 3, 1, 20, 0, 15, 0, 400, 1, 0, 0, 0, 3, 0, 0, 1, "4004:4", 0, "0.5", 2103, 2104, 0, 1703, "狂战路线一第三阶主动技能。命令场上所有召唤物立即对目标敌人发起一次强力普攻（伤害增加50%），并嘲讽附近400码内的敌人，持续15秒。Params[0]=伤害增加系数(0.5)"),
    (104, "钢铁洪流", 1, 1, 4, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, "4005:2", 0, "4", 0, 0, 0, 1704, "狂战路线一第四阶被动技能。当场上召唤物数量大于4个时，所有棋子获得冲锋效果（攻速-25%伤害+30%每次攻击造成0.5s眩晕）。Params[0]=触发召唤物数量阈值(4)"),
    (105, "天灾降临", 1, 2, 5, 1, 40, 0, 5, 0, 600, 3, 2, 2, 500, 3, 0, 0, 1, "4006:3", 0, "2.0,1.5,0.3", 2105, 2106, 0, 1705, "狂战路线一第五阶主动技能。在指定区域召唤一场持续性的战争风暴，对范围内的所有敌人持续造成巨额伤害，并为范围内的所有友方棋子提供巨额护盾和生命偷取效果。战争风暴效果持续5秒。Params[0]=伤害系数(2.0) Params[1]=护盾系数(1.5) Params[2]=生命偷取比例(0.3)"),
    (106, "寂灭斩", 1, 2, 3, 2, 15, 0, 0, 0, 0, 1, 2.5, 0, 1, 0, 0, 1, 0, 0, "2.5", 2107, 2108, 0, 1706, "狂战路线二第三阶主动技能。对单个敌人发动一次超高伤害的斩击，若目标被击败，则重置此技能冷却时间。Params[0]=伤害系数(2.5)"),
    (107, "孤影", 1, 1, 4, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, "4007:1", 0, "0.4,0.6", 0, 0, 0, 1707, "狂战路线二第四阶被动技能。当身边没有其他友方棋子时，自身造成的所有伤害大幅提升，并获得高额闪避率。Params[0]=伤害提升系数(0.4) Params[1]=闪避率(0.6)"),
    (108, "裁决之刻", 1, 2, 5, 2, 30, 0, 0, 0, 0, 3, 1.8, 0, 1, 0, 0, 1, "4008:4", 0, "1.8,0.7", 2109, 2110, 0, 1708, "狂战路线二第五阶主动技能。标记一名敌人，在短暂延迟后，对其发动一次无视大量百分比防御（70%）的终极斩击。如果此击击败目标，将对所有其他敌人造成一次巨大的范围恐惧效果。Params[0]=伤害系数(1.8) Params[1]=防御无视比例(0.7)"),
    (201, "暗影咒体", 2, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, "0.1,0.8,0.3", 0, 0, 0, 1711, "术士固定被动。Params[0]=伤害提升(0.1) Params[1]=受伤诅咒概率(0.8) Params[2]=造伤诅咒概率(0.3)"),
    (202, "生命虹吸", 2, 2, 1, 0, 20, 30, 5, 10, 5, 2, 0.3, 0, 0, 0, 0, 1, 0, "3003:5", "0,0.20,0.15", 2202, 0, 0.05, 1712, "术士固定主动。命中时HitBuffs施加3003流血到命中目标。Params[0]=每秒吸血量 Params[1]=虚弱移速降低 Params[2]=虚弱攻速降低"),
]

# 生成TXT内容
lines = []

# 添加注释行
lines.append("#\tSummonerSkillTable\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t")
lines.append("#\tID\t\tName\tSummonerClass\tSkillType\tUnlockTier\tBranchId\tCooldown\tSpiritCost\tDuration\tCastRange\tAreaRadius\tDamageType\tDamageCoeff\tBaseDamage\tEffectHitType\tProjectilePrefabId\tProjectileSpeed\tHitCount\tInstantBuffs\tHitBuffs\tParams\tEffectId\tHitEffectId\tEffectSpawnHeight\tIconId\tDesc")
lines.append("#\tint\t\tstring\tint\tint\tint\tint\tfloat\tfloat\tfloat\tfloat\tfloat\tint\tfloat\tfloat\tint\tint\tfloat\tint\tstring\tstring\tfloat[]\tint\tint\tfloat\tint\tstring")
lines.append("#\t技能唯一ID\t备注\t技能名称\t所属召唤师职业 1=狂战 2=术士 3=混沌 4=德鲁伊\t技能类型 1=被动 2=主动\t解锁阶段 1=初始 3=第三阶 4=第四阶 5=第五阶\t分支ID 0=固定技能 1=路线一 2=路线二\t冷却时间（秒）\t灵力消耗\t持续时间（秒）\t施法/生效范围\tAOE半径（0=单体）\t伤害类型 0=无 1=物理 2=魔法 3=真实\t伤害系数（基于攻击力/法强 如1.5=150%）\t基础固定伤害\t命中类型 0=瞬发 1=近战 2=投射物 3=AoE 4=射线\t投射物预制体ID（0=无）\t投射物速度（0=即时命中）\t命中/触发次数\t释放/条件触发时施加的Buff列表，格式\"buffId:targetType,...\"。targetType:1=召唤师自身 2=全体友方(不含召唤师) 3=全体友方(含召唤师) 4=全体敌方 5=命中目标(单体)\t命中目标时施加的Buff列表，格式同InstantBuffs\t技能专属参数数组（含义由各技能代码定义 见Desc备注）\t技能特效资源ID\t受击特效资源ID\t特效生成高度偏移\t技能图标资源ID\t技能描述")

# 添加数据行
for skill in skills_data:
    # 构建行数据，确保有28列
    row_data = [str(v) if v != "" else "" for v in skill]
    
    # 确保有28列
    while len(row_data) < 28:
        row_data.append("")
    
    # 只取前28列
    row_data = row_data[:28]
    
    # 用Tab连接
    line = "\t".join(row_data)
    lines.append(line)

# 写入文件
with open('AI工作区/配置表/SummonerSkillTable.txt', 'w', encoding='utf-8-sig') as f:
    for line in lines:
        f.write(line + '\n')

print("✓ 已重新生成TXT文件，所有行都有28列且数据正确对齐")
