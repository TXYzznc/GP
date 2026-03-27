using System;
using Newtonsoft.Json.Linq;

/// <summary>
/// 特殊效果配置数据
/// </summary>
[Serializable]
public class SpecialEffectData
{
    public int Id; // 效果ID
    public string Name; // 效果名称
    public string Description; // 效果描述
    public SpecialEffectType EffectType; // 效果类型
    public string EffectParams; // 效果参数（JSON格式）

    private JObject m_ParsedParams; // 解析后的参数

    /// <summary>
    /// 获取解析后的参数对象
    /// </summary>
    public JObject GetParams()
    {
        if (m_ParsedParams == null && !string.IsNullOrEmpty(EffectParams))
        {
            try
            {
                m_ParsedParams = JObject.Parse(EffectParams);
            }
            catch (Exception e)
            {
                DebugEx.Error("SpecialEffectData", $"解析效果参数失败 ID:{Id}, Error:{e.Message}");
                m_ParsedParams = new JObject();
            }
        }
        return m_ParsedParams ?? new JObject();
    }

    /// <summary>
    /// 获取参数值
    /// </summary>
    public T GetParamValue<T>(string key, T defaultValue = default)
    {
        var paramsObj = GetParams();
        if (paramsObj.ContainsKey(key))
        {
            try
            {
                return paramsObj[key].ToObject<T>();
            }
            catch
            {
                DebugEx.Warning("SpecialEffectData", $"获取参数失败 ID:{Id}, Key:{key}");
            }
        }
        return defaultValue;
    }
}
