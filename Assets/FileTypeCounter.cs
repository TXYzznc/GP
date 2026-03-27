using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class FileTypeCounter : MonoBehaviour
{
    // 在 Inspector 面板中设置目标文件夹路径（相对于项目根目录，如 "Assets/Models"）
    public string targetFolderPath = "Assets";

    [ContextMenu("开始统计文件类型")]
    public void CountFileTypes()
    {
        // 获取绝对路径
        string fullPath = Path.Combine(Application.dataPath, "..", targetFolderPath);

        if (!Directory.Exists(fullPath))
        {
            Debug.LogError($"路径不存在: {fullPath}");
            return;
        }

        // 存储后缀名和对应的数量
        Dictionary<string, int> extensionCounts = new Dictionary<string, int>();

        // 获取所有文件（SearchOption.AllDirectories 表示递归查找）
        string[] allFiles = Directory.GetFiles(fullPath, "*.*", SearchOption.AllDirectories);

        foreach (string file in allFiles)
        {
            // 获取后缀名（包含点，如 .meta, .png）
            string ext = Path.GetExtension(file).ToLower();

            if (string.IsNullOrEmpty(ext))
            {
                ext = "(无后缀)";
            }

            if (extensionCounts.ContainsKey(ext))
            {
                extensionCounts[ext]++;
            }
            else
            {
                extensionCounts[ext] = 1;
            }
        }

        // 输出结果
        Debug.Log($"<b>统计报告 - 文件夹: {targetFolderPath}</b>\n总文件数: {allFiles.Length}");
        foreach (var kvp in extensionCounts)
        {
            Debug.Log($"类型: <color=yellow>{kvp.Key}</color> | 数量: {kvp.Value}");
        }
    }
}