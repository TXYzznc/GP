using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

/// <summary>
/// 地图对象按钮
/// 代表一个可传送的场景，显示场景信息并处理传送逻辑
/// </summary>
public partial class MapItemUI : UIItemBase
{
    private int m_SceneId;
    private SceneTable m_SceneData;
    private PlayerSaveData m_PlayerSaveData;
    private bool m_IsUnlocked;

    private void Awake()
    {
        if (varBtn != null)
        {
            varBtn.onClick.AddListener(OnMapItemClicked);
        }
    }

    private void OnDestroy()
    {
        if (varBtn != null)
        {
            varBtn.onClick.RemoveListener(OnMapItemClicked);
        }
    }

    /// <summary>
    /// 初始化 MapItemUI，绑定对应的场景ID
    /// </summary>
    public void Initialize(int sceneId)
    {
        m_SceneId = sceneId;
        m_SceneData = GF.DataTable.GetDataTable<SceneTable>().GetDataRow(sceneId);

        if (m_SceneData == null)
        {
            Log.Error($"MapItemUI: 场景ID {sceneId} 不存在于 SceneTable");
            return;
        }

        // 获取玩家存档数据
        m_PlayerSaveData = PlayerAccountDataManager.Instance.CurrentSaveData;

        RefreshUI();
    }

    /// <summary>
    /// 刷新UI，根据场景是否解锁来显示不同的UI状态
    /// </summary>
    private void RefreshUI()
    {
        if (m_SceneData == null)
            return;

        // 判断场景是否已解锁
        m_IsUnlocked = CheckSceneUnlocked();

        // 更新场景名称
        if (varMapName != null)
        {
            varMapName.text = m_SceneData.DisplayName;
        }

        // 显示/隐藏 Mask（场景未解锁时显示遮罩）
        if (varMask != null)
        {
            varMask.gameObject.SetActive(!m_IsUnlocked);
        }

        // 按钮交互状态
        if (varBtn != null)
        {
            varBtn.interactable = m_IsUnlocked;
        }
    }

    /// <summary>
    /// 检查场景是否已解锁
    /// </summary>
    private bool CheckSceneUnlocked()
    {
        if (m_SceneData == null || m_PlayerSaveData == null)
            return false;

        return m_SceneData.CheckCondition(m_PlayerSaveData);
    }

    /// <summary>
    /// 鼠标进入时显示交互信息UI
    /// </summary>
    public void OnPointerEnter()
    {
        if (varInteractUI != null)
        {
            varInteractUI.gameObject.SetActive(true);

            // 如果未解锁，显示解锁条件
            if (!m_IsUnlocked && m_SceneData != null)
            {
                string conditionMsg = m_SceneData.GetConditionNotMetMessage();
                // 可以在 InteractUI 中显示条件文本
                Log.Info($"场景 {m_SceneData.DisplayName} 解锁条件：{conditionMsg}");
            }
        }
    }

    /// <summary>
    /// 鼠标离开时隐藏交互信息UI
    /// </summary>
    public void OnPointerExit()
    {
        if (varInteractUI != null)
        {
            varInteractUI.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 地图项被点击
    /// </summary>
    private void OnMapItemClicked()
    {
        if (!m_IsUnlocked)
        {
            Log.Warning($"场景 {m_SceneId} 未解锁");
            return;
        }

        if (m_SceneData == null)
            return;

        // 加载场景
        string sceneName = m_SceneData.SceneName;
        Log.Info($"传送到场景: {sceneName}");

        // 关闭 OverworldUI（获取父UI）
        UIFormBase parentUI = GetComponentInParent<UIFormBase>();
        if (parentUI != null)
        {
            GF.UI.CloseUIForm(parentUI.UIForm);
        }

        // 加载场景
        GF.Scene.LoadScene(sceneName);
    }
}
