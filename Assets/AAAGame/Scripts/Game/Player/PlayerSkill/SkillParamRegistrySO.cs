using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Skills/ParamRegistry")]
public class SkillParamRegistrySO : ScriptableObject
{
    public List<SkillParamSO> allParams;
    private Dictionary<int, SkillParamSO> cache;

    public T Get<T>(int skillId) where T : SkillParamSO
    {
        cache ??= Build();
        return cache.TryGetValue(skillId, out var p) ? p as T : null;
    }

    public SkillParamSO Get(int skillId)
    {
        cache ??= Build();
        return cache.TryGetValue(skillId, out var p) ? p : null;
    }

    private Dictionary<int, SkillParamSO> Build()
    {
        var dic = new Dictionary<int, SkillParamSO>();
        foreach (var p in allParams) if (p) dic[p.SkillId] = p;
        return dic;
    }
}