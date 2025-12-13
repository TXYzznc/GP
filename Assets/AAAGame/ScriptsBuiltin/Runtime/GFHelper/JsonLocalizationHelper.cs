using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;
using GameFramework.Localization;
[Obfuz.ObfuzIgnore]
public class JsonLocalizationHelper : DefaultLocalizationHelper
{
    public override bool ParseData(ILocalizationManager localizationManager, string dictionaryString, object userData)
    {
        var dic = Utility.Json.ToObject<Dictionary<string, string>>(dictionaryString);
        if (dic == null)
        {
            return false;
        }
        foreach (KeyValuePair<string, string> item in dic)
        {
            // 修复：检查 Value 是否为 null
            if (string.IsNullOrEmpty(item.Value))
            {
                Log.Warning($"多语言Key '{item.Key}' 的值为空，已跳过");
                continue; // 跳过空值
            }

            localizationManager.AddRawString(item.Key, System.Text.RegularExpressions.Regex.Unescape(item.Value));
        }
        return true;
    }
}
