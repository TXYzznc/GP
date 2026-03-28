#!/usr/bin/env python3
# -*- coding: utf-8 -*-

# 定义表头
headers = [
    "ID", "Name", "SummonerClass", "SkillType", "UnlockTier", "BranchId", 
    "Cooldown", "SpiritCost", "Duration", "CastRange", "AreaRadius", 
    "DamageType", "DamageCoeff", "BaseDamage", "EffectHitType", 
    "ProjectilePrefabId", "ProjectileSpeed", "HitCount", "InstantBuffs", 
    "HitBuffs", "Params", "EffectId", "HitEffectId", "EffectSpawnHeight", 
    "IconId", "Desc"
]

# 定义技能数据（字典形式，确保每个字段都有值）
skills = [
    {
        "ID": 101, "Name": "狂怒之心", "SummonerClass": 1, "SkillType": 1, 
        "UnlockTier": 1, "BranchId": 0, "Cooldown": 0, "SpiritCost": 0, 
        "Duration": 0, "CastRange": 0, "AreaRadius": 0, "DamageType": 0, 
        "DamageCoeff": 0, "BaseDamage": 0, "EffectHitType": 0, 
        "ProjectilePrefabId": 0, "ProjectileSpeed": 0, "HitCount": 1, 
        "InstantBuffs": "4003:3", "HitBuffs": 0, "Params": 0.5, 
        "EffectId": 0, "HitEffectId": 0, "EffectSpawnHeight": 0, 
        "IconId": 1701, "Desc": "狂战固定被动。条件触发：战场有友方HP<Params[0]时，InstantBuffs施加（4003→全体友方含召唤师）；BerserkerRageBuff自动检测目标是否为召唤师并应用30%伤害加成。条件解除时移除"
    },
    {
        "ID": 102, "Name": "战意激昂", "SummonerClass": 1, "SkillType": 2, 
        "UnlockTier": 1, "BranchId": 0, "Cooldown": 30, "SpiritCost": 0, 
        "Duration": 10, "CastRange": 0, "AreaRadius": 0, "DamageType": 0, 
        "DamageCoeff": 0, "BaseDamage": 0, "EffectHitType": 0, 
        "ProjectilePrefabId": 0, "ProjectileSpeed": 0, "HitCount": 1, 
        "InstantBuffs": "4001:3", "HitBuffs": 0, "Params": 20, 
        "EffectId": 2102, "HitEffectId": 0, "EffectSpawnHeight": 0, 
        "IconId": 1702, "Desc": "狂战固定主动。Params[0]=HP消耗(20)。扣除召唤师HP后，InstantBuffs施加战意激昂Buff（攻速+20%伤害+15%）到全体友方（含召唤师），持续10秒"
    },
    {
        "ID": 103, "Name": "王者号令", "SummonerClass": 1, "SkillType": 2, 
        "UnlockTier": 3, "BranchId": 1, "Cooldown": 20, "SpiritCost": 0, 
        "Duration": 15, "CastRange": 0, "AreaRadius": 400, "DamageType": 1, 
        "DamageCoeff": 0, "BaseDamage": 0, "EffectHitType": 3, 
        "ProjectilePrefabId": 0, "ProjectileSpeed": 0, "HitCount": 1, 
        "InstantBuffs": "4004:4", "HitBuffs": 0, "Params": 0.5, 
        "EffectId": 2103, "HitEffectId": 2104, "EffectSpawnHeight": 0, 
        "IconId": 1703, "Desc": "狂战路线一第三阶主动技能。命令场上所有召唤物立即对目标敌人发起一次强力普攻（伤害增加50%），并嘲讽附近400码内的敌人，持续15秒。Params[0]=伤害增加系数(0.5)"
    },
    {
        "ID": 104, "Name": "钢铁洪流", "SummonerClass": 1, "SkillType": 1, 
        "UnlockTier": 4, "BranchId": 1, "Cooldown": 0, "SpiritCost": 0, 
        "Duration": 0, "CastRange": 0, "AreaRadius": 0, "DamageType": 0, 
        "DamageCoeff": 0, "BaseDamage": 0, "EffectHitType": 0, 
        "ProjectilePrefabId": 0, "ProjectileSpeed": 0, "HitCount": 1, 
        "InstantBuffs": "4005:2", "HitBuffs": 0, "Params": 4, 
        "EffectId": 0, "HitEffectId": 0, "EffectSpawnHeight": 0, 
        "IconId": 1704, "Desc": "狂战路线一第四阶被动技能。当场上召唤物数量大于4个时，所有棋子获得冲锋效果（攻速-25%伤害+30%每次攻击造成0.5s眩晕）。Params[0]=触发召唤物数量阈值(4)"
    },
    {
        "ID": 105, "Name": "天灾降临", "SummonerClass": 1, "SkillType": 2, 
        "UnlockTier": 5, "BranchId": 1, "Cooldown": 40, "SpiritCost": 0, 
        "Duration": 5, "CastRange": 0, "AreaRadius": 600, "DamageType": 2, 
        "DamageCoeff": 2, "BaseDamage": 500, "EffectHitType": 3, 
        "ProjectilePrefabId": 0, "ProjectileSpeed": 0, "HitCount": 1, 
        "InstantBuffs": "4006:3", "HitBuffs": 0, "Params": "2.0,1.5,0.3", 
        "EffectId": 2105, "HitEffectId": 2106, "EffectSpawnHeight": 0, 
        "IconId": 1705, "Desc": "狂战路线一第五阶主动技能。在指定区域召唤一场持续性的战争风暴，对范围内的所有敌人持续造成巨额伤害，并为范围内的所有友方棋子提供巨额护盾和生命偷取效果。战争风暴效果持续5秒。Params[0]=伤害系数(2.0) Params[1]=护盾系数(1.5) Params[2]=生命偷取比例(0.3)"
    },
    {
        "ID": 106, "Name": "寂灭斩", "SummonerClass": 1, "SkillType": 2, 
        "UnlockTier": 3, "BranchId": 2, "Cooldown": 15, "SpiritCost": 0, 
        "Duration": 0, "CastRange": 0, "AreaRadius": 0, "DamageType": 1, 
        "DamageCoeff": 2.5, "BaseDamage": 0, "EffectHitType": 1, 
        "ProjectilePrefabId": 0, "ProjectileSpeed": 0, "HitCount": 1, 
        "InstantBuffs": 0, "HitBuffs": 0, "Params": 2.5, 
        "EffectId": 2107, "HitEffectId": 2108, "EffectSpawnHeight": 0, 
        "IconId": 1706, "Desc": "狂战路线二第三阶主动技能。对单个敌人发动一次超高伤害的斩击，若目标被击败，则重置此技能冷却时间。Params[0]=伤害系数(2.5)"
    },
    {
        "ID": 107, "Name": "孤影", "SummonerClass": 1, "SkillType": 1, 
        "UnlockTier": 4, "BranchId": 2, "Cooldown": 0, "SpiritCost": 0, 
        "Duration": 0, "CastRange": 0, "AreaRadius": 0, "DamageType": 0, 
        "DamageCoeff": 0, "BaseDamage": 0, "EffectHitType": 0, 
        "ProjectilePrefabId": 0, "ProjectileSpeed": 0, "HitCount": 1, 
        "InstantBuffs": "4007:1", "HitBuffs": 0, "Params": "0.4,0.6", 
        "EffectId": 0, "HitEffectId": 0, "EffectSpawnHeight": 0, 
        "IconId": 1707, "Desc": "狂战路线二第四阶被动技能。当身边没有其他友方棋子时，自身造成的所有伤害大幅提升，并获得高额闪避率。Params[0]=伤害提升系数(0.4) Params[1]=闪避率(0.6)"
    },
    {
        "ID": 108, "Name": "裁决之刻", "SummonerClass": 1, "SkillType": 2, 
        "UnlockTier": 5, "BranchId": 2, "Cooldown": 30, "SpiritCost": 0, 
        "Duration": 0, "CastRange": 0, "AreaRadius": 0, "DamageType": 3, 
        "DamageCoeff": 1.8, "BaseDamage": 0, "EffectHitType": 1, 
        "ProjectilePrefabId": 0, "ProjectileSpeed": 0, "HitCount": 1, 
        "InstantBuffs": "4008:4", "HitBuffs": 0, "Params": "1.8,0.7", 
        "EffectId": 2109, "HitEffectId": 2110, "EffectSpawnHeight": 0, 
        "IconId": 1708, "Desc": "狂战路线二第五阶主动技能。标记一名敌人，在短暂延迟后，对其发动一次无视大量百分比防御（70%）的终极斩击。如果此击击败目标，将对所有其他敌人造成一次巨大的范围恐惧效果。Params[0]=伤害系数(1.8) Params[1]=防御无视比例(0.7)"
    },
    {
        "ID": 201, "Name": "暗影咒体", "SummonerClass": 2, "SkillType": 1, 
        "UnlockTier": 1, "BranchId": 0, "Cooldown": 0, "SpiritCost": 0, 
        "Duration": 0, "CastRange": 0, "AreaRadius": 0, "DamageType": 0, 
        "DamageCoeff": 0, "BaseDamage": 0, "EffectHitType": 0, 
        "ProjectilePrefabId": 0, "ProjectileSpeed": 0, "HitCount": 1, 
        "InstantBuffs": 0, "HitBuffs": 0, "Params": "0.1,0.8,0.3", 
        "EffectId": 0, "HitEffectId": 0, "EffectSpawnHeight": 0, 
        "IconId": 1711, "Desc": "术士固定被动。Params[0]=伤害提升(0.1) Params[1]=受伤诅咒概率(0.8) Params[2]=造伤诅咒概率(0.3)"
    },
    {
        "ID": 202, "Name": "生命虹吸", "SummonerClass": 2, "SkillType": 2, 
        "UnlockTier": 1, "BranchId": 0, "Cooldown": 20, "SpiritCost": 30, 
        "Duration": 5, "CastRange": 10, "AreaRadius": 5, "DamageType": 2, 
        "DamageCoeff": 0.3, "BaseDamage": 0, "EffectHitType": 0, 
        "ProjectilePrefabId": 0, "ProjectileSpeed": 0, "HitCount": 1, 
        "InstantBuffs": 0, "HitBuffs": "3003:5", "Params": "0,0.20,0.15", 
        "EffectId": 2202, "HitEffectId": 0, "EffectSpawnHeight": 0.05, 
        "IconId": 1712, "Desc": "术士固定主动。命中时HitBuffs施加3003流血到命中目标。Params[0]=每秒吸血量 Params[1]=虚弱移速降低 Params[2]=虚弱攻速降低"
    }
]

# 生成TXT文件
output_lines = []

# 添加表名行
output_lines.append("#\tSummonerSkillTable" + "\t" * 24)

# 添加字段名行
field_names = ["ID", "", "Name", "SummonerClass", "SkillType", "UnlockTier", "BranchId", 
               "Cooldown", "SpiritCost", "Duration", "CastRange", "AreaRadius", 
               "DamageType", "DamageCoeff", "BaseDamage", "EffectHitType", 
               "ProjectilePrefabId", "ProjectileSpeed", "HitCount", "InstantBuffs", 
               "HitBuffs", "Params", "EffectId", "HitEffectId", "EffectSpawnHeight", 
               "IconId", "Desc"]
output_lines.append("#\t" + "\t".join(field_names))

# 添加类型行
field_types = ["int", "", "string", "int", "int", "int", "int", 
               "float", "float", "float", "float", "float", 
               "int", "float", "float", "int", 
               "int", "float", "int", "string", 
               "string", "float[]", "int", "int", "float", 
               "int", "string"]
output_lines.append("#\t" + "\t".join(field_types))

# 添加注释行
comments = ["技能唯一ID", "备注", "技能名称", "所属召唤师职业 1=狂战 2=术士 3=混沌 4=德鲁伊", 
            "技能类型 1=被动 2=主动", "解锁阶段 1=初始 3=第三阶 4=第四阶 5=第五阶", 
            "分支ID 0=固定技能 1=路线一 2=路线二", "冷却时间（秒）", "灵力消耗", 
            "持续时间（秒）", "施法/生效范围", "AOE半径（0=单体）", 
            "伤害类型 0=无 1=物理 2=魔法 3=真实", "伤害系数（基于攻击力/法强 如1.5=150%）", 
            "基础固定伤害", "命中类型 0=瞬发 1=近战 2=投射物 3=AoE 4=射线", 
            "投射物预制体ID（0=无）", "投射物速度（0=即时命中）", "命中/触发次数", 
            "释放/条件触发时施加的Buff列表，格式\"buffId:targetType,...\"。targetType:1=召唤师自身 2=全体友方(不含召唤师) 3=全体友方(含召唤师) 4=全体敌方 5=命中目标(单体)", 
            "命中目标时施加的Buff列表，格式同InstantBuffs", "技能专属参数数组（含义由各技能代码定义 见Desc备注）", 
            "技能特效资源ID", "受击特效资源ID", "特效生成高度偏移", 
            "技能图标资源ID", "技能描述"]
output_lines.append("#\t" + "\t".join(comments))

# 添加数据行
for skill in skills:
    row = [str(skill["ID"]), ""]  # ID和备注列
    for header in headers[1:]:  # 跳过ID，因为已经添加了
        value = skill.get(header, "")
        row.append(str(value))
    output_lines.append("\t".join(row))

# 写入文件
with open("D:/Sourcetree/Clash_Of_Gods/AI工作区/配置表/SummonerSkillTable.txt", "w", encoding="utf-8") as f:
    f.write("\n".join(output_lines))

print("✅ 配置表生成成功！")
print(f"总共生成 {len(skills)} 个技能")
