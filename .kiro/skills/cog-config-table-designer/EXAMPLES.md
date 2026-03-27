# 配置表示例集合

## 基础类型配置表

```
#	ItemTable													
#	ID		Name	Price	IsStackable	CreateTime
#	int		string	double	bool	datetime
#	物品ID	备注	名称	价格	可堆叠	创建时间
	1		生命药水	50.0	true	2024-01-01 10:00:00
	2		魔法药水	75.5	true	2024-01-02 15:30:00
```

## Unity类型配置表

```
#	EffectTable													
#	ID		Position	Rotation	Color	Scale
#	int		vector3	quaternion	color	vector2
#	特效ID	备注	位置	旋转	颜色	缩放
	1		0,1,0	0,0,0,1	1.0,0.5,0.0,1.0	1.5,1.5
	2		2,0,0	0.707,0,0,0.707	0.0,1.0,0.0,1.0	2.0,2.0
```

## 数组类型配置表

```
#	SkillTable													
#	ID		Damages	Positions	Requirements
#	int		int[]	vector3[]	string[]
#	技能ID	备注	伤害值	作用点	需求条件
	1		100,150,200	0,0,0|1,0,0|2,0,0	Level,Mana,Weapon
	2		80,120,160	0,1,0|1,1,0|2,1,0	Level,Health,Shield
```

## 枚举类型配置表

```
#	EnumTable													
#	ID		Type	Quality	Rarity
#	int		enum	enum	enum
#	ID编号	备注	类型	品质	稀有度
	1		ItemType.Weapon	Quality.Epic	Rarity.Rare
	2		ItemType.Armor	Quality.Legendary	Rarity.Common
```

## 装备效果配置表（复杂示例）

```
#	EquipmentEffectTable													
#	Id		ItemId	BaseAttack	BaseMaxHP	BaseDefense	BaseMagicPower	BaseCritRate	BaseCritDamage	BaseAttackSpeed	BaseMoveSpeed	BaseHPRegen	AffixPoolIds	MinAffixCount	MaxAffixCount	SpecialEffectId	RequireLevel	SellPrice
#	int		int	int	int	int	int	float	float	float	float	float	string	int	int	int	int	int
#	效果ID	备注	物品ID	基础攻击力	基础生命值	基础防御力	基础魔法强度	基础暴击率	基础暴击伤害	基础攻击速度	基础移动速度	基础生命恢复	词条池ID列表	最小词条数	最大词条数	特殊效果ID	需求等级	出售价格
	40001		40001	50	0	0	0	0.0	0.0	0.0	0.0	0.0	2001,2003,2004,2008	2	4	4001	10	5000
	40002		40002	0	500	80	0	0.0	0.0	0.0	0.0	10.0	2002,2006,2009	2	4	4002	15	6000
```
