using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework.Event;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using DG.Tweening;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class PlayerSkillUI : StateAwareUIForm
{
    #region 私有字段

    private PlayerSkillManager m_SkillManager;
    private List<PlayerSkillSlot> m_SlotList = new List<PlayerSkillSlot>();
    private Dictionary<int, float> m_LastCooldownState = new Dictionary<int, float>();

    #endregion

    #region 事件订阅

    protected override void SubscribeEvents()
    {
        Log.Info("PlayerSkillUI: 订阅局内和战斗状态事件");
        // 订阅局内事件（进入 → 显示）
        GF.Event.Subscribe(InGameEnterEventArgs.EventId, OnInGameEnter);
        GF.Event.Subscribe(InGameLeaveEventArgs.EventId, OnInGameLeave);

        // 订阅战斗事件（探索 → 战斗）
        GF.Event.Subscribe(CombatEnterEventArgs.EventId, OnCombatEnter);
        GF.Event.Subscribe(CombatLeaveEventArgs.EventId, OnCombatLeave);

        GF.Event.Subscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
        GF.Event.Subscribe(ExplorationLeaveEventArgs.EventId, OnExplorationLeave);
    }

    protected override void UnsubscribeEvents()
    {
        Log.Info("PlayerSkillUI: 取消订阅局内和战斗状态事件");
        // 取消订阅局内事件
        GF.Event.Unsubscribe(InGameEnterEventArgs.EventId, OnInGameEnter);
        GF.Event.Unsubscribe(InGameLeaveEventArgs.EventId, OnInGameLeave);

        // 取消订阅战斗事件
        GF.Event.Unsubscribe(CombatEnterEventArgs.EventId, OnCombatEnter);
        GF.Event.Unsubscribe(CombatLeaveEventArgs.EventId, OnCombatLeave);

        GF.Event.Unsubscribe(ExplorationEnterEventArgs.EventId, OnExplorationEnter);
        GF.Event.Unsubscribe(ExplorationLeaveEventArgs.EventId, OnExplorationLeave);
    }

    #endregion

    #region 事件处理

    private void OnInGameEnter(object sender, GameEventArgs e)
    {
        Log.Info("PlayerSkillUI: 收到局内进入事件 → 显示UI");
        ShowUI();
    }

    private void OnInGameLeave(object sender, GameEventArgs e)
    {
        Log.Info("PlayerSkillUI: 收到局内离开事件 → 隐藏UI");
        HideUI();
    }

    private void OnExplorationEnter(object sender, GameEventArgs e)
    {
        Log.Info("PlayerSkillUI: 收到探索进入事件 → 显示UI");
        ShowUI();
        RefreshSkills();
    }

    private void OnExplorationLeave(object sender, GameEventArgs e)
    {
        Log.Info("PlayerSkillUI: 收到探索离开事件 → 隐藏UI");
        HideUI();
    }

    private void OnCombatEnter(object sender, GameEventArgs e)
    {
        Log.Info("PlayerSkillUI: 收到战斗进入事件 → 隐藏UI");
        HideUI();
    }

    private void OnCombatLeave(object sender, GameEventArgs e)
    {
        Log.Info("PlayerSkillUI: 收到战斗离开事件 → 显示UI");
        ShowUI();
    }

    #endregion

    #region 生命周期

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        // 初始化冷却状态记录
        m_LastCooldownState.Clear();
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        // 初始化槽位（先创建空槽位）
        InitializeSlots();

        // 异步等待并绑定技能
        WaitAndBindSkillsAsync().Forget();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        // 清理槽位
        ClearAllSlots();

        base.OnClose(isShutdown, userData);
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);

        // 只有技能管理器存在时才更新
        if (
            m_SkillManager != null
            && m_SkillManager.Skills != null
            && m_SkillManager.Skills.Count > 0
        )
        {
            // 更新所有槽位显示
            UpdateAllSlots();

            // 检测冷却完成
            CheckCooldownComplete();
        }
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化槽位
    /// </summary>
    private void InitializeSlots()
    {
        m_SlotList.Clear();

        if (varSkillSlotContainer == null || varSkillSlot == null)
        {
            DebugEx.Error("[PlayerSkillUI] 槽位容器或槽位预制体未配置！");
            return;
        }

        // 创建3个槽位
        for (int i = 0; i < 3; i++)
        {
            GameObject slotObj = Instantiate(varSkillSlot, varSkillSlotContainer);
            slotObj.SetActive(true);

            PlayerSkillSlot slot = slotObj.GetComponent<PlayerSkillSlot>();
            if (slot == null)
            {
                DebugEx.Error($"[PlayerSkillUI] 槽位{i}没有PlayerSkillSlot组件！");
                continue;
            }

            m_SlotList.Add(slot);
        }

        DebugEx.Log($"[PlayerSkillUI] 初始化了 {m_SlotList.Count} 个槽位");
    }

    #endregion

    #region 技能绑定

    /// <summary>
    /// 异步等待技能管理器并绑定技能
    /// </summary>
    private async UniTaskVoid WaitAndBindSkillsAsync()
    {
        try
        {
            DebugEx.Log("[PlayerSkillUI] 开始等待技能管理器...");

            // 等待技能管理器出现（最多等待10秒）
            var cts = new System.Threading.CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            await UniTask.WaitUntil(
                () => FindObjectOfType<PlayerSkillManager>() != null,
                cancellationToken: cts.Token
            );

            m_SkillManager = FindObjectOfType<PlayerSkillManager>();
            DebugEx.Log("[PlayerSkillUI] ✓ 找到技能管理器");

            // 等待技能加载完成（最多等待5秒）
            cts = new System.Threading.CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            await UniTask.WaitUntil(
                () => m_SkillManager.Skills != null && m_SkillManager.Skills.Count > 0,
                cancellationToken: cts.Token
            );

            DebugEx.Log($"[PlayerSkillUI] ✓ 技能加载完成，共 {m_SkillManager.Skills.Count} 个技能");

            // 绑定技能
            BindSkills();
        }
        catch (OperationCanceledException)
        {
            DebugEx.Error("[PlayerSkillUI] ✗✗ 等待技能超时！检查角色是否正确生成");
        }
        catch (System.Exception ex)
        {
            DebugEx.Error($"[PlayerSkillUI] ✗ 绑定技能时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 绑定技能到槽位
    /// </summary>
    private void BindSkills()
    {
        if (m_SkillManager == null || m_SkillManager.Skills == null)
        {
            DebugEx.Warning("[PlayerSkillUI] 技能管理器或技能列表为空");
            return;
        }

        // 清理所有槽位
        ClearAllSlots();

        // 从数据表获取技能配置数据并绑定到槽位
        var dataTable = GF.DataTable.GetDataTable<PlayerSkillTable>();
        if (dataTable == null)
        {
            DebugEx.Error("[PlayerSkillUI] PlayerSkillTable 数据表未加载！");
            return;
        }

        // 遍历技能管理器中的技能
        foreach (var skill in m_SkillManager.Skills)
        {
            // 从数据表获取技能配置
            var skillRow = dataTable.GetDataRow(skill.SkillId);
            if (skillRow == null)
            {
                DebugEx.Warning($"[PlayerSkillUI] 找不到技能配置: SkillId={skill.SkillId}");
                continue;
            }

            // 构建技能配置
            SkillCommonConfig config = new SkillCommonConfig
            {
                Id = skillRow.Id,
                Name = skillRow.Name,
                Desc = skillRow.Desc,
                Cooldown = skillRow.Cooldown,
                Cost = skillRow.Cost,
                IconId = skillRow.IconId,
                SlotIndex = skillRow.SlotIndex,
            };

            // 验证槽位索引（1-3）
            if (config.SlotIndex < 1 || config.SlotIndex > 3)
            {
                DebugEx.Warning(
                    $"[PlayerSkillUI] 技能{config.Name}的SlotIndex={config.SlotIndex}无效！"
                );
                continue;
            }

            // 绑定到对应槽位（数组索引0-2）
            int arrayIndex = config.SlotIndex - 1;
            if (arrayIndex < m_SlotList.Count)
            {
                m_SlotList[arrayIndex].BindSkill(skill, config, config.SlotIndex);

                // 初始化冷却状态
                m_LastCooldownState[arrayIndex] = 0f;

                DebugEx.Log($"[PlayerSkillUI] 绑定技能: {config.Name} 到槽位{config.SlotIndex}");
            }
        }
    }

    /// <summary>
    /// 刷新技能列表（当技能变化时调用）
    /// </summary>
    public void RefreshSkills()
    {
        // 重新异步绑定
        WaitAndBindSkillsAsync().Forget();
    }

    #endregion

    #region 槽位显示

    /// <summary>
    /// 更新所有槽位显示
    /// </summary>
    private void UpdateAllSlots()
    {
        foreach (var slot in m_SlotList)
        {
            slot?.RefreshDisplay();
        }
    }

    /// <summary>
    /// 清理所有槽位
    /// </summary>
    private void ClearAllSlots()
    {
        foreach (var slot in m_SlotList)
        {
            slot?.Clear();
        }

        m_LastCooldownState.Clear();
    }

    #endregion

    #region 冷却检测

    /// <summary>
    /// 检测冷却完成
    /// </summary>
    private void CheckCooldownComplete()
    {
        if (m_SkillManager == null || m_SkillManager.Skills == null)
            return;

        for (int i = 0; i < m_SlotList.Count; i++)
        {
            var slot = m_SlotList[i];
            if (slot == null || !slot.HasSkill())
                continue;

            // 获取当前冷却时间（通过反射）
            float currentCd = GetSkillCooldown(i);

            // 检测是否刚完成冷却
            if (m_LastCooldownState.TryGetValue(i, out float lastCd))
            {
                if (lastCd > 0f && currentCd <= 0f)
                {
                    // 冷却刚完成
                    OnCooldownComplete(i);
                }
            }

            // 更新状态
            m_LastCooldownState[i] = currentCd;
        }
    }

    /// <summary>
    /// 获取技能冷却时间（通过反射）
    /// </summary>
    private float GetSkillCooldown(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= m_SkillManager.Skills.Count)
            return 0f;

        var skill = m_SkillManager.Skills[slotIndex];
        if (skill == null)
            return 0f;

        try
        {
            var cdField = skill
                .GetType()
                .GetField(
                    "cdRemain",
                    System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance
                );

            if (cdField != null)
            {
                object value = cdField.GetValue(skill);
                if (value is float cdRemain)
                {
                    return cdRemain;
                }
            }
        }
        catch (System.Exception ex)
        {
            DebugEx.Error($"[PlayerSkillUI] 获取技能冷却时间失败: {ex.Message}");
        }

        return 0f;
    }

    /// <summary>
    /// 冷却完成回调
    /// </summary>
    private void OnCooldownComplete(int slotIndex)
    {
        // 播放冷却完成音效（可选）
        // GF.Sound.PlayEffect("ui/cooldown_complete.wav");

        DebugEx.Log($"[PlayerSkillUI] 槽位{slotIndex + 1}冷却完成");
    }

    #endregion

    #region 公共接口

    /// <summary>
    /// 获取槽位
    /// </summary>
    public PlayerSkillSlot GetSlot(int index)
    {
        if (index >= 0 && index < m_SlotList.Count)
        {
            return m_SlotList[index];
        }
        return null;
    }

    /// <summary>
    /// 获取所有槽位
    /// </summary>
    public List<PlayerSkillSlot> GetAllSlots()
    {
        return m_SlotList;
    }

    #endregion

    #region 调试

#if UNITY_EDITOR
    [ContextMenu("刷新技能列表")]
    private void DebugRefreshSkills()
    {
        if (Application.isPlaying)
        {
            RefreshSkills();
        }
    }

    [ContextMenu("清理所有槽位")]
    private void DebugClearAllSlots()
    {
        if (Application.isPlaying)
        {
            ClearAllSlots();
        }
    }

    [ContextMenu("打印槽位信息")]
    private void DebugPrintSlots()
    {
        if (!Application.isPlaying)
        {
            DebugEx.Log("请在运行时使用此功能");
            return;
        }

        DebugEx.Log("=== 技能槽位信息 ===");
        for (int i = 0; i < m_SlotList.Count; i++)
        {
            var slot = m_SlotList[i];
            if (slot != null && slot.HasSkill())
            {
                var config = slot.GetSkillConfig();
                DebugEx.Log(
                    $"槽位{i + 1}: {config.Name} (ID:{config.Id}, 冷却:{config.Cooldown}s, 消耗:{config.Cost})"
                );
            }
            else
            {
                DebugEx.Log($"槽位{i + 1}: 空");
            }
        }
    }
#endif

    #endregion

    #region 动画

    protected new void ShowUI()
    {
        var cg = GetComponent<CanvasGroup>();
        var rt = GetComponent<RectTransform>();
        if (cg == null) { base.ShowUI(); return; }
        DOTween.Kill(gameObject);
        cg.alpha = 0f; cg.blocksRaycasts = true; cg.interactable = true;
        var orig = rt.anchoredPosition;
        rt.anchoredPosition = orig + new Vector2(0, -60f);
        DOTween.Sequence().SetUpdate(true)
            .Join(cg.DOFade(1f, 0.3f).SetEase(Ease.OutQuart))
            .Join(rt.DOAnchorPos(orig, 0.3f).SetEase(Ease.OutQuart));
    }

    protected new void HideUI()
    {
        var cg = GetComponent<CanvasGroup>();
        var rt = GetComponent<RectTransform>();
        if (cg == null) { base.HideUI(); return; }
        DOTween.Kill(gameObject);
        var orig = rt.anchoredPosition;
        DOTween.Sequence().SetUpdate(true)
            .Join(cg.DOFade(0f, 0.2f).SetEase(Ease.InQuart))
            .Join(rt.DOAnchorPos(orig + new Vector2(0, -60f), 0.2f).SetEase(Ease.InQuart))
            .OnComplete(() => { cg.interactable = false; cg.blocksRaycasts = false; });
    }

    #endregion
}
