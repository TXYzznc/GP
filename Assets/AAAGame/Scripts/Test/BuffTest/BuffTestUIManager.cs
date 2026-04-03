using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

/// <summary>
/// Buff 测试工具 UI 管理器
/// 提供便捷的交互界面
/// </summary>
public class BuffTestUIManager : MonoBehaviour
{
    #region 字段

    private GameObject m_UIRoot;
    private GameObject m_CurrentTarget;

    // UI 组件引用
    private Text m_TargetNameText;
    private Dropdown m_BuffSelectionDropdown;
    private Button m_ApplyButton;
    private Button m_ClearAllButton;
    private Button m_CloseButton;
    private Dropdown m_PresetDropdown;
    private Button m_ApplyPresetButton;
    private Text m_BuffListText;
    private Text m_AttributeInfoText;

    // UI 状态
    private bool m_IsUIVisible = false;
    private CanvasGroup m_UICanvasGroup;

    #endregion

    #region 生命周期

    private void Awake()
    {
        CreateUI();
        RegisterInputs();
    }

    private void OnDestroy()
    {
        UnregisterInputs();
    }

    #endregion

    #region 初始化 UI

    /// <summary>
    /// 创建 UI 界面
    /// </summary>
    private void CreateUI()
    {
        // 创建 Canvas
        var canvasGO = new GameObject("BuffTestToolCanvas");
        canvasGO.transform.SetParent(transform);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        // 创建背景面板
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform);
        var bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.8f);
        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bgGO.SetActive(false);

        // 创建主面板
        var panelGO = new GameObject("BuffTestPanel");
        panelGO.transform.SetParent(canvasGO.transform);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.anchoredPosition = new Vector2(-200, -200);
        panelRect.sizeDelta = new Vector2(400, 600);

        var panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        // 添加 LayoutGroup
        var layoutGroup = panelGO.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        layoutGroup.spacing = 8;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = false;

        m_UICanvasGroup = panelGO.AddComponent<CanvasGroup>();
        m_UIRoot = panelGO;

        // 创建标题
        CreateTitle(panelGO);

        // 创建目标选择区域
        CreateTargetSection(panelGO);

        // 创建快速施加区域
        CreateApplySection(panelGO);

        // 创建预设区域
        CreatePresetSection(panelGO);

        // 创建 Buff 列表显示
        CreateBuffListSection(panelGO);

        // 创建属性信息显示
        CreateAttributeSection(panelGO);

        // 创建关闭按钮
        CreateCloseButton(panelGO);

        DebugEx.LogModule("BuffTestUIManager", "UI 创建完成");
    }

    /// <summary>
    /// 创建标题
    /// </summary>
    private void CreateTitle(GameObject parent)
    {
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(parent.transform);

        var titleText = titleGO.AddComponent<Text>();
        titleText.text = "📊 Buff 测试工具";
        titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        titleText.fontSize = 18;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.color = Color.white;

        var titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(380, 30);
    }

    /// <summary>
    /// 创建目标选择区域
    /// </summary>
    private void CreateTargetSection(GameObject parent)
    {
        var sectionGO = new GameObject("TargetSection");
        sectionGO.transform.SetParent(parent.transform);

        var sectionRect = sectionGO.GetComponent<RectTransform>();
        sectionRect.sizeDelta = new Vector2(380, 50);

        // 标签
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(sectionGO.transform);
        var labelText = labelGO.AddComponent<Text>();
        labelText.text = "当前目标: ";
        labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        labelText.fontSize = 12;
        labelText.color = Color.white;
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(-180, 10);
        labelRect.sizeDelta = new Vector2(80, 20);

        // 目标名称
        var targetGO = new GameObject("TargetName");
        targetGO.transform.SetParent(sectionGO.transform);
        m_TargetNameText = targetGO.AddComponent<Text>();
        m_TargetNameText.text = "未选择";
        m_TargetNameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        m_TargetNameText.fontSize = 12;
        m_TargetNameText.color = Color.yellow;
        var targetRect = targetGO.GetComponent<RectTransform>();
        targetRect.anchoredPosition = new Vector2(0, 10);
        targetRect.sizeDelta = new Vector2(200, 20);
    }

    /// <summary>
    /// 创建快速施加区域
    /// </summary>
    private void CreateApplySection(GameObject parent)
    {
        var sectionGO = new GameObject("ApplySection");
        sectionGO.transform.SetParent(parent.transform);
        var sectionRect = sectionGO.GetComponent<RectTransform>();
        sectionRect.sizeDelta = new Vector2(380, 80);

        // Buff 选择下拉菜单
        var dropdownGO = new GameObject("BuffDropdown");
        dropdownGO.transform.SetParent(sectionGO.transform);
        m_BuffSelectionDropdown = dropdownGO.AddComponent<Dropdown>();
        var dropdownRect = dropdownGO.GetComponent<RectTransform>();
        dropdownRect.sizeDelta = new Vector2(360, 30);

        var dropdownImage = dropdownGO.AddComponent<Image>();
        dropdownImage.color = new Color(0.2f, 0.2f, 0.2f);

        // 初始化 Buff 列表
        UpdateBuffDropdown();

        // 应用按钮
        var applyGO = CreateButton(sectionGO, "ApplyButton", "[应用]", new Vector2(-175, -35), 80);
        m_ApplyButton = applyGO.GetComponent<Button>();
        m_ApplyButton.onClick.AddListener(OnApplyBuffClicked);

        // 清空按钮
        var clearGO = CreateButton(sectionGO, "ClearButton", "[清空]", new Vector2(0, -35), 80);
        var clearButton = clearGO.GetComponent<Button>();
        clearButton.onClick.AddListener(OnClearAllClicked);
    }

    /// <summary>
    /// 创建预设区域
    /// </summary>
    private void CreatePresetSection(GameObject parent)
    {
        var sectionGO = new GameObject("PresetSection");
        sectionGO.transform.SetParent(parent.transform);
        var sectionRect = sectionGO.GetComponent<RectTransform>();
        sectionRect.sizeDelta = new Vector2(380, 60);

        // 预设标签
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(sectionGO.transform);
        var labelText = labelGO.AddComponent<Text>();
        labelText.text = "预设方案:";
        labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        labelText.fontSize = 12;
        labelText.color = Color.white;
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(-175, 15);
        labelRect.sizeDelta = new Vector2(80, 20);

        // 预设下拉菜单
        var dropdownGO = new GameObject("PresetDropdown");
        dropdownGO.transform.SetParent(sectionGO.transform);
        m_PresetDropdown = dropdownGO.AddComponent<Dropdown>();
        var dropdownRect = dropdownGO.GetComponent<RectTransform>();
        dropdownRect.anchoredPosition = new Vector2(0, 15);
        dropdownRect.sizeDelta = new Vector2(280, 30);

        var dropdownImage = dropdownGO.AddComponent<Image>();
        dropdownImage.color = new Color(0.2f, 0.2f, 0.2f);

        // 初始化预设列表
        UpdatePresetDropdown();

        // 应用预设按钮
        var applyGO = CreateButton(sectionGO, "ApplyPresetButton", "[应用]", new Vector2(0, -25), 60);
        m_ApplyPresetButton = applyGO.GetComponent<Button>();
        m_ApplyPresetButton.onClick.AddListener(OnApplyPresetClicked);
    }

    /// <summary>
    /// 创建 Buff 列表显示区域
    /// </summary>
    private void CreateBuffListSection(GameObject parent)
    {
        var sectionGO = new GameObject("BuffListSection");
        sectionGO.transform.SetParent(parent.transform);
        var sectionRect = sectionGO.GetComponent<RectTransform>();
        sectionRect.sizeDelta = new Vector2(380, 120);

        // 标签
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(sectionGO.transform);
        var labelText = labelGO.AddComponent<Text>();
        labelText.text = "当前 Buff 列表:";
        labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        labelText.fontSize = 12;
        labelText.color = Color.white;

        // 列表显示
        var listGO = new GameObject("BuffList");
        listGO.transform.SetParent(sectionGO.transform);
        m_BuffListText = listGO.AddComponent<Text>();
        m_BuffListText.text = "无 Buff";
        m_BuffListText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        m_BuffListText.fontSize = 10;
        m_BuffListText.color = Color.cyan;
        m_BuffListText.horizontalOverflow = HorizontalWrapMode.Wrap;

        var listRect = listGO.GetComponent<RectTransform>();
        listRect.sizeDelta = new Vector2(360, 100);
    }

    /// <summary>
    /// 创建属性信息显示区域
    /// </summary>
    private void CreateAttributeSection(GameObject parent)
    {
        var sectionGO = new GameObject("AttributeSection");
        sectionGO.transform.SetParent(parent.transform);
        var sectionRect = sectionGO.GetComponent<RectTransform>();
        sectionRect.sizeDelta = new Vector2(380, 100);

        // 标签
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(sectionGO.transform);
        var labelText = labelGO.AddComponent<Text>();
        labelText.text = "属性信息:";
        labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        labelText.fontSize = 12;
        labelText.color = Color.white;

        // 属性显示
        var infoGO = new GameObject("AttributeInfo");
        infoGO.transform.SetParent(sectionGO.transform);
        m_AttributeInfoText = infoGO.AddComponent<Text>();
        m_AttributeInfoText.text = "未选择目标";
        m_AttributeInfoText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        m_AttributeInfoText.fontSize = 10;
        m_AttributeInfoText.color = Color.green;
        m_AttributeInfoText.horizontalOverflow = HorizontalWrapMode.Wrap;

        var infoRect = infoGO.GetComponent<RectTransform>();
        infoRect.sizeDelta = new Vector2(360, 80);
    }

    /// <summary>
    /// 创建关闭按钮
    /// </summary>
    private void CreateCloseButton(GameObject parent)
    {
        var closeGO = CreateButton(parent, "CloseButton", "[关闭]", Vector2.zero, 80);
        m_CloseButton = closeGO.GetComponent<Button>();
        m_CloseButton.onClick.AddListener(ToggleUI);
    }

    /// <summary>
    /// 创建按钮辅助方法
    /// </summary>
    private GameObject CreateButton(GameObject parent, string name, string text, Vector2 pos, float width)
    {
        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent.transform);

        var btnImage = btnGO.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.5f, 0.8f);

        var btnText = new GameObject("Text");
        btnText.transform.SetParent(btnGO.transform);
        var textComponent = btnText.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComponent.fontSize = 12;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.color = Color.white;

        var textRect = btnText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var btnRect = btnGO.GetComponent<RectTransform>();
        btnRect.anchoredPosition = pos;
        btnRect.sizeDelta = new Vector2(width, 30);

        btnGO.AddComponent<Button>();

        return btnGO;
    }

    #endregion

    #region 更新方法

    /// <summary>
    /// 更新 Buff 下拉菜单
    /// </summary>
    private void UpdateBuffDropdown()
    {
        if (m_BuffSelectionDropdown == null)
            return;

        var options = new List<Dropdown.OptionData>();
        var buffList = BuffTestTool.Instance.GetAllAvailableBuffs();

        foreach (var buff in buffList)
        {
            options.Add(new Dropdown.OptionData($"{buff.Name} (ID={buff.BuffId})"));
        }

        m_BuffSelectionDropdown.options = options;
    }

    /// <summary>
    /// 更新预设下拉菜单
    /// </summary>
    private void UpdatePresetDropdown()
    {
        if (m_PresetDropdown == null)
            return;

        var options = new List<Dropdown.OptionData>();
        var presets = BuffPresetManager.Instance.GetAllPresets();

        foreach (var preset in presets)
        {
            options.Add(new Dropdown.OptionData(preset.Name));
        }

        m_PresetDropdown.options = options;
    }

    /// <summary>
    /// 刷新当前 Buff 列表显示
    /// </summary>
    private void RefreshBuffListDisplay()
    {
        if (m_CurrentTarget == null || m_BuffListText == null)
        {
            m_BuffListText.text = "未选择目标";
            return;
        }

        var buffs = BuffEffectVerifier.Instance.GetBuffDetails(m_CurrentTarget);
        if (buffs.Count == 0)
        {
            m_BuffListText.text = "无 Buff";
            return;
        }

        var sb = new System.Text.StringBuilder();
        foreach (var buff in buffs)
        {
            sb.AppendLine($"• {buff.Name} (堆叠={buff.StackCount})");
        }

        m_BuffListText.text = sb.ToString();
    }

    /// <summary>
    /// 刷新属性信息显示
    /// </summary>
    private void RefreshAttributeDisplay()
    {
        if (m_CurrentTarget == null || m_AttributeInfoText == null)
        {
            m_AttributeInfoText.text = "未选择目标";
            return;
        }

        var attr = BuffEffectVerifier.Instance.GetTargetAttributes(m_CurrentTarget);
        var (buffCount, debuffCount) = BuffEffectVerifier.Instance.GetBuffAndDebuffCount(m_CurrentTarget);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"HP: {attr.HP:F0}/{attr.MaxHP:F0}");
        sb.AppendLine($"MP: {attr.MP:F0}/{attr.MaxMP:F0}");
        sb.AppendLine($"攻击: {attr.AtkDamage:F0}");
        sb.AppendLine($"防御: {attr.PhysDef:F0}");
        sb.AppendLine($"增益/减益: {buffCount}/{debuffCount}");

        m_AttributeInfoText.text = sb.ToString();
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 应用 Buff 点击事件
    /// </summary>
    private void OnApplyBuffClicked()
    {
        if (m_CurrentTarget == null)
        {
            DebugEx.WarningModule("BuffTestUIManager", "请先选择目标");
            return;
        }

        var selectedIndex = m_BuffSelectionDropdown.value;
        var buffList = BuffTestTool.Instance.GetAllAvailableBuffs();

        if (selectedIndex >= 0 && selectedIndex < buffList.Count)
        {
            var buffId = buffList[selectedIndex].BuffId;
            BuffTestTool.Instance.ApplyBuffToTarget(buffId, m_CurrentTarget);
            RefreshBuffListDisplay();
            RefreshAttributeDisplay();
        }
    }

    /// <summary>
    /// 清空所有 Buff 点击事件
    /// </summary>
    private void OnClearAllClicked()
    {
        if (m_CurrentTarget == null)
        {
            DebugEx.WarningModule("BuffTestUIManager", "请先选择目标");
            return;
        }

        BuffTestTool.Instance.ClearAllBuffs(m_CurrentTarget);
        RefreshBuffListDisplay();
        RefreshAttributeDisplay();
    }

    /// <summary>
    /// 应用预设点击事件
    /// </summary>
    private void OnApplyPresetClicked()
    {
        if (m_CurrentTarget == null)
        {
            DebugEx.WarningModule("BuffTestUIManager", "请先选择目标");
            return;
        }

        var selectedIndex = m_PresetDropdown.value;
        var presets = BuffPresetManager.Instance.GetAllPresets();

        if (selectedIndex >= 0 && selectedIndex < presets.Count)
        {
            var presetName = presets[selectedIndex].Name;
            BuffPresetManager.Instance.ApplyPreset(presetName, m_CurrentTarget);
            RefreshBuffListDisplay();
            RefreshAttributeDisplay();
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置当前测试目标
    /// </summary>
    public void SetTarget(GameObject target)
    {
        m_CurrentTarget = target;

        if (m_TargetNameText != null)
        {
            m_TargetNameText.text = target != null ? target.name : "未选择";
        }

        RefreshBuffListDisplay();
        RefreshAttributeDisplay();

        if (target != null)
        {
            DebugEx.LogModule("BuffTestUIManager", $"设置测试目标: {target.name}");
        }
    }

    /// <summary>
    /// 切换 UI 显示状态
    /// </summary>
    public void ToggleUI()
    {
        m_IsUIVisible = !m_IsUIVisible;

        if (m_UIRoot != null)
        {
            m_UIRoot.SetActive(m_IsUIVisible);
        }

        DebugEx.LogModule("BuffTestUIManager", $"UI 已{(m_IsUIVisible ? "打开" : "关闭")}");
    }

    /// <summary>
    /// 显示 UI
    /// </summary>
    public void ShowUI()
    {
        m_IsUIVisible = true;
        if (m_UIRoot != null)
        {
            m_UIRoot.SetActive(true);
        }
    }

    /// <summary>
    /// 隐藏 UI
    /// </summary>
    public void HideUI()
    {
        m_IsUIVisible = false;
        if (m_UIRoot != null)
        {
            m_UIRoot.SetActive(false);
        }
    }

    #endregion

    #region 输入处理

    /// <summary>
    /// 注册输入处理
    /// </summary>
    private void RegisterInputs()
    {
        if (PlayerInputManager.Instance == null)
            return;

        // Ctrl+B 打开/关闭工具
        // TODO: 根据实际的 PlayerInputManager API 调整
    }

    /// <summary>
    /// 注销输入处理
    /// </summary>
    private void UnregisterInputs()
    {
        // 清理输入处理
    }

    #endregion
}
