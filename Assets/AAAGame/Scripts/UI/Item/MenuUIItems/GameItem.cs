using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

/// <summary>
/// 存档信息UI项
/// </summary>
public partial class GameItem : UIItemBase
{
    private SaveBriefInfo m_SaveInfo;
    private Action<string> m_OnChooseCallback;

    /// <summary>
    /// 初始化存档项
    /// </summary>
    public void Init(SaveBriefInfo saveInfo, Action<string> onChooseCallback)
    {
        m_SaveInfo = saveInfo;
        m_OnChooseCallback = onChooseCallback;

        UpdateDisplay();
    }

    /// <summary>
    /// 更新显示
    /// </summary>
    private void UpdateDisplay()
    {
        if (m_SaveInfo == null) return;

        // 1. 存档名称（玩家名称）
        if (varNameText != null)
        {
            varNameText.text = m_SaveInfo.SaveName;
        }

        // 2. 最后修改时间
        if (varTimeText != null)
        {
            DateTime lastPlayTime = new DateTime((long)m_SaveInfo.LastPlayTime);
            varTimeText.text = lastPlayTime.ToString("yyyy-MM-dd HH:mm");
        }

        // 3. 章节（暂时显示"第一章"，待完善）
        if (varCharacter != null)
        {
            varCharacter.text = "第一章";
        }

        // 4. 游戏时间（暂时显示"Day 1"，待完善）
        if (varDay != null)
        {
            varDay.text = "Day 1";
        }

        // 5. 召唤师职业
        if (varOccupationText != null)
        {
            // 从存档数据加载召唤师名称
            string occupationName = GetOccupationName(m_SaveInfo.SaveId);
            varOccupationText.text = occupationName;
        }

        // 6. 召唤师等级
        if (varGradeText != null)
        {
            varGradeText.text = $"Lv.{m_SaveInfo.GlobalLevel}";
        }

        // 绑定选择按钮
        if (varChoose != null)
        {
            varChoose.onClick.RemoveAllListeners();
            varChoose.onClick.AddListener(OnChooseClick);
        }
    }

    /// <summary>
    /// 获取职业名称
    /// </summary>
    private string GetOccupationName(string saveId)
    {
        try
        {
            // 先尝试从存档数据获取召唤师信息
            var saveData = PlayerAccountDataManager.Instance.GetSaveBriefInfo(saveId);
            if (saveData != null)
            {
                // 需要从完整存档中读取召唤师ID
                var fullSaveData = LoadFullSaveData(saveId);
                if (fullSaveData != null)
                {
                    var summonerTable = GF.DataTable.GetDataTable<SummonerTable>();
                    if (summonerTable != null)
                    {
                        var summoner = summonerTable.GetDataRow(fullSaveData.CurrentSummonerId);
                        if (summoner != null)
                        {
                            return summoner.Name;
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"获取职业名称失败: {e.Message}");
        }

        return "未知职业";
    }

    /// <summary>
    /// 加载完整存档数据（仅用于读取信息，不设置为当前存档）
    /// </summary>
    private PlayerSaveData LoadFullSaveData(string saveId)
    {
        try
        {
            string accountId = "000001"; // 暂时固定
            string filePath = System.IO.Path.Combine(
                Application.persistentDataPath,
                "PlayerSaves",
                accountId,
                $"Save_{saveId}.json"
            );

            if (System.IO.File.Exists(filePath))
            {
                string json = System.IO.File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                return JsonUtility.FromJson<PlayerSaveData>(json);
            }
        }
        catch (Exception e)
        {
            Log.Error($"加载完整存档数据失败: {e.Message}");
        }

        return null;
    }

    /// <summary>
    /// 选择按钮点击
    /// </summary>
    private void OnChooseClick()
    {
        Log.Info($"选择存档: {m_SaveInfo.SaveName} (ID: {m_SaveInfo.SaveId})");
        m_OnChooseCallback?.Invoke(m_SaveInfo.SaveId);
    }

    /// <summary>
    /// 清理
    /// </summary>
    private void OnDestroy()
    {
        if (varChoose != null)
        {
            varChoose.onClick.RemoveAllListeners();
        }

        m_SaveInfo = null;
        m_OnChooseCallback = null;
    }
}
