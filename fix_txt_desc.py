#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
修复TXT文件，为缺少Desc列的行添加描述信息
"""

# 读取原始文件
with open('AI工作区/配置表/SummonerSkillTable.txt', 'r', encoding='utf-8-sig') as f:
    lines = f.readlines()

# 定义需要添加Desc的行及其内容
desc_data = {
    6: '狂战路线一第三阶主动技能。命令场上所有召唤物立即对目标敌人发起一次强力普攻（伤害增加50%），并嘲讽附近400码内的敌人，持续15秒。Params[0]=伤害增加系数(0.5)',
    7: '狂战路线一第四阶被动技能。当场上召唤物数量大于4个时，所有棋子获得冲锋效果（攻速-25%伤害+30%每次攻击造成0.5s眩晕）。Params[0]=触发召唤物数量阈值(4)',
    8: '狂战路线一第五阶主动技能。在指定区域召唤一场持续性的战争风暴，对范围内的所有敌人持续造成巨额伤害，并为范围内的所有友方棋子提供巨额护盾和生命偷取效果。战争风暴效果持续5秒。Params[0]=伤害系数(2.0) Params[1]=护盾系数(1.5) Params[2]=生命偷取比例(0.3)',
    10: '狂战路线二第四阶被动技能。当身边没有其他友方棋子时，自身造成的所有伤害大幅提升，并获得高额闪避率。Params[0]=伤害提升系数(0.4) Params[1]=闪避率(0.6)',
}

# 修复这些行
for line_idx, desc in desc_data.items():
    line = lines[line_idx].rstrip('\n\r')
    lines[line_idx] = line + '\t' + desc + '\n'

# 写回文件
with open('AI工作区/配置表/SummonerSkillTable.txt', 'w', encoding='utf-8-sig') as f:
    f.writelines(lines)

print("✓ 已修复TXT文件，为ID 103-105、107添加了Desc列")
