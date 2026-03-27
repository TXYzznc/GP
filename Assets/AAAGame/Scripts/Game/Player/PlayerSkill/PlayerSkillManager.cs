using System.Collections.Generic;
using UnityEngine;

public class PlayerSkillManager : MonoBehaviour
{
    public List<IPlayerSkill> Skills { get; private set; } = new();

    [SerializeField]
    private SkillParamRegistrySO paramRegistry;

    private PlayerSkillContext ctx;
    private GameObject m_PlayerCharacter; // 玩家角色对象

    private void Awake()
    {
        ctx = new PlayerSkillContext
        {
            Owner = gameObject,
            Transform = transform,
            Controller = GetComponent<PlayerController>(),
        };
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 更新所有技能的冷却
        for (int i = 0; i < Skills.Count; i++)
            Skills[i].Tick(dt);

        // 检测技能输入（槽位1-3对应J/K/L按键）
        if (PlayerInputManager.Instance != null)
        {
            for (int slot = 1; slot <= 3; slot++)
            {
                if (PlayerInputManager.Instance.SkillDown(slot))
                {
                    // 查找对应槽位的技能
                    IPlayerSkill skill = FindSkillBySlot(slot);
                    if (skill != null)
                    {
                        skill.TryCast();
                    }
                }
            }
        }
    }

    /// <summary>
    /// 根据槽位索引查找技能
    /// </summary>
    private IPlayerSkill FindSkillBySlot(int slotIndex)
    {
        for (int i = 0; i < Skills.Count; i++)
        {
            if (Skills[i].SkillId != 0)
            {
                // 从配置表获取槽位信息
                var tb = GF.DataTable.GetDataTable<PlayerSkillTable>();
                var row = tb?.GetDataRow(Skills[i].SkillId);
                if (row != null && row.SlotIndex == slotIndex)
                {
                    return Skills[i];
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 设置玩家角色（由GameProcedure调用）
    /// </summary>
    public void SetPlayerCharacter(GameObject character)
    {
        m_PlayerCharacter = character;

        // 更新技能上下文
        if (character != null)
        {
            ctx = new PlayerSkillContext
            {
                Owner = character,
                Transform = character.transform,
                Controller = character.GetComponent<PlayerController>(),
            };

            DebugEx.Log("[PlayerSkillManager] 已设置玩家角色");
        }
    }

    /// <summary>
    /// 设置技能参数注册表（由GameProcedure调用）
    /// </summary>
    public void SetParamRegistry(SkillParamRegistrySO registry)
    {
        paramRegistry = registry;
        DebugEx.Log("[PlayerSkillManager] 技能参数注册表已设置");
    }

    /// <summary>
    /// 根据玩家数据中的已解锁技能ID，动态刷新技能列表
    /// </summary>
    public void UpdateSkillsFromPlayerData(IReadOnlyList<int> playerSkillIds)
    {
        Skills.Clear();
        if (playerSkillIds == null)
            return;

        for (int i = 0; i < playerSkillIds.Count; i++)
        {
            int id = playerSkillIds[i];

            // 1) 通用配置（冷却/消耗/名称/描述等）
            var common = LoadCommonConfig(id);
            if (common.Id == 0)
                continue;

            // 2) 用工厂创建技能实例（无法反射）
            var skill = SkillFactory.Create(id);
            if (skill == null)
            {
                DebugEx.Error($"SkillFactory.Create failed, skillId={id} (未注册?)");
                continue;
            }

            // 3) 初始化并加入列表
            // 注意：这里统一获取param
            SkillParamSO param = paramRegistry != null ? paramRegistry.Get(id) : null;
            skill.Init(ctx, common, param);

            Skills.Add(skill);
        }
    }

    private SkillCommonConfig LoadCommonConfig(int skillId)
    {
        var tb = GF.DataTable.GetDataTable<PlayerSkillTable>();
        var row = tb?.GetDataRow(skillId);
        if (row == null)
        {
            DebugEx.Error($"PlayerSkillTable missing skillId={skillId}");
            return default;
        }

        return new SkillCommonConfig
        {
            Id = row.Id,
            Name = row.Name,
            Desc = row.Desc,
            Cooldown = row.Cooldown,
            Cost = row.Cost,
            IconId = row.IconId,
            SlotIndex = row.SlotIndex,
        };
    }
}
