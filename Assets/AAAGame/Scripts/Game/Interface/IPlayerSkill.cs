using System.Collections.Generic;
using UnityEngine;
public interface IPlayerSkill
{
    int SkillId { get; }

    void Init(PlayerSkillContext ctx, SkillCommonConfig common, SkillParamSO _param);
    void Tick(float dt);

    /// <summary>尝试释放，内部判断冷却、蓝等</summary>
    bool TryCast();
}
