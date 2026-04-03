using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 战斗准备界面 - 棋子槽位UI项
/// 使用 Button 监听点击，避免与 Input.GetMouseButtonDown 冲突
/// </summary>
public partial class ChessItemUI : UIItemBase, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    #region 字段

    private string m_InstanceId; // 棋子实例ID
    private Action<string> m_OnSelectCallback; // 点击回调
    private Action<string> m_OnDragBeginCallback; // 拖拽开始回调
    private Action<string> m_OnDragEndCallback; // 拖拽结束回调
    private CanvasGroup m_CanvasGroup;
    private bool m_IsDragging; // 是否正在拖拽
    #endregion

    #region 生命周期

    protected override void OnInit()
    {
        base.OnInit();

        m_CanvasGroup = GetComponent<CanvasGroup>();
        if (m_CanvasGroup == null)
        {
            m_CanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 使用 Button 监听点击事件
        if (varBtn != null)
        {
            varBtn.onClick.AddListener(OnButtonClick);
        }

        // 订阅死亡状态重置事件（回基地时刷新 UI 显示）
        ChessDeploymentTracker.Instance.OnDeathStateReset += RefreshDeployStatus;
        // 订阅棋子死亡事件（战斗中死亡时实时刷新 Mask）
        ChessDeploymentTracker.Instance.OnChessDied += OnChessDiedHandler;
    }

    private void OnDestroy()
    {
        if (varBtn != null)
        {
            varBtn.onClick.RemoveListener(OnButtonClick);
        }

        // 取消订阅
        if (ChessDeploymentTracker.Instance != null)
        {
            ChessDeploymentTracker.Instance.OnDeathStateReset -= RefreshDeployStatus;
            ChessDeploymentTracker.Instance.OnChessDied -= OnChessDiedHandler;
        }
    }

    /// <summary>
    /// 棋子死亡事件回调：只刷新自身对应的那个实例
    /// </summary>
    private void OnChessDiedHandler(string deadInstanceId)
    {
        if (deadInstanceId == m_InstanceId)
            RefreshDeployStatus();
    }

    /// <summary>
    /// 刷新出战/死亡状态显示（不重新加载图片资源）
    /// </summary>
    private void RefreshDeployStatus()
    {
        var instance = ChessDeploymentTracker.Instance.GetInstance(m_InstanceId);
        if (instance == null) return;

        if (varText != null)
        {
            if (instance.IsDead)
            {
                varText.text = "已死亡";
                varText.gameObject.SetActive(true);
            }
            else if (instance.IsDeployed)
            {
                varText.text = "已出战";
                varText.gameObject.SetActive(true);
            }
            else
            {
                varText.gameObject.SetActive(false);
            }
        }

        if (varMask != null)
        {
            varMask.SetActive(instance.IsDeployed || instance.IsDead);
        }
    }

    #endregion

    #region 数据设置

    /// <summary>
    /// 设置棋子数据
    /// </summary>
    /// <param name="instanceId">棋子实例ID</param>
    /// <param name="chessId">棋子配置ID</param>
    /// <param name="onSelectCallback">点击回调</param>
    /// <param name="onDragEndCallback">拖拽结束回调</param>
    /// <param name="onDragBeginCallback">拖拽开始回调</param>
    public void SetData(
        string instanceId,
        int chessId,
        Action<string> onSelectCallback = null,
        Action<string> onDragEndCallback = null,
        Action<string> onDragBeginCallback = null
    )
    {
        m_InstanceId = instanceId;
        m_OnSelectCallback = onSelectCallback;
        m_OnDragBeginCallback = onDragBeginCallback;
        m_OnDragEndCallback = onDragEndCallback;

        // 获取配置
        if (ChessDataManager.Instance.TryGetConfig(chessId, out var config))
        {
            // 设置名称
            if (varNameText != null)
            {
                varNameText.text = config.Name;
            }

            // 设置星级
            if (varStar != null)
            {
                varStar.text = new string('★', config.StarLevel);
            }

            // 加载图标（异步）
            LoadIconAsync(config.IconId);

            // ⭐ 根据稀有度设置卡牌框、背景和名字背景
            SetQualityUI(config.Quality);

            // 刷新出战/死亡状态显示
            RefreshDeployStatus();

            DebugEx.LogModule(
                "ChessItemUI",
                $"SetData: chessId={chessId}, name={config.Name}, star={config.StarLevel}, quality={config.Quality}"
            );
        }
        else
        {
            DebugEx.ErrorModule(
                "ChessItemUI",
                $"SetData failed: config not found for chessId={chessId}"
            );
        }
    }

    /// <summary>
    /// 根据稀有度设置卡牌框、背景和名字背景
    /// </summary>
    private async void SetQualityUI(int quality)
    {
        // 稀有度映射到资源ID
        int cardFrameId = 19000 + quality;
        int bgId = 19010 + quality;
        int maskId = 19020 + quality;

        // 加载卡牌框
        if (varCardFrame != null)
        {
            await GameExtension.ResourceExtension.LoadSpriteAsync(cardFrameId, varCardFrame);
            DebugEx.LogModule(
                "ChessItemUI",
                $"SetQualityUI: 加载卡牌框 quality={quality}, resourceId={cardFrameId}"
            );
        }

        // 加载卡片背景
        if (varBg != null)
        {
            await GameExtension.ResourceExtension.LoadSpriteAsync(bgId, varBg);
            DebugEx.LogModule(
                "ChessItemUI",
                $"SetQualityUI: 加载卡片背景 quality={quality}, resourceId={bgId}"
            );
        }

        // 加载名字背景
        if (varNameBg != null)
        {
            var maskImage = varNameBg.GetComponent<Image>();
            if (maskImage != null)
            {
                await GameExtension.ResourceExtension.LoadSpriteAsync(maskId, maskImage);
                DebugEx.LogModule(
                    "ChessItemUI",
                    $"SetQualityUI: 加载名字背景 quality={quality}, resourceId={maskId}"
                );
            }
        }
    }

    /// <summary>
    /// 异步加载图标
    /// </summary>
    private async void LoadIconAsync(int iconResourceId)
    {
        if (varImage == null)
            return;

        await GameExtension.ResourceExtension.LoadSpriteAsync(iconResourceId, varImage);
    }

    /// <summary>
    /// 设置遮罩显示状态（已出战/未出战）
    /// </summary>
    public void SetDeployedState()
    {
        RefreshDeployStatus();
    }

    #endregion

    #region Button 点击

    private void OnButtonClick()
    {
        // 如果正在拖拽，忽略点击
        if (m_IsDragging)
            return;

        // 检查实例数据
        var instance = ChessDeploymentTracker.Instance.GetInstance(m_InstanceId);
        if (instance == null)
        {
            DebugEx.ErrorModule(
                "ChessItemUI",
                $"OnButtonClick: 实例不存在 instanceId={m_InstanceId}"
            );
            return;
        }

        // ⭐ 检查是否已死亡
        if (instance.IsDead)
        {
            DebugEx.LogModule(
                "ChessItemUI",
                $"OnButtonClick: 棋子已死亡，无法选中 instanceId={m_InstanceId}"
            );
            return;
        }

        // ⭐ 修改：已出战的棋子也可以点击显示详情（由回调方决定如何处理）
        // 触发选中回调（无论已出战还是未出战）
        m_OnSelectCallback?.Invoke(m_InstanceId);
        DebugEx.LogModule("ChessItemUI", $"OnButtonClick: 选中棋子 instanceId={m_InstanceId}, IsDeployed={instance.IsDeployed}");
    }

    #endregion

    #region 拖拽接口

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 检查实例数据
        var instance = ChessDeploymentTracker.Instance.GetInstance(m_InstanceId);
        if (instance == null)
        {
            DebugEx.ErrorModule(
                "ChessItemUI",
                $"OnBeginDrag: 实例不存在 instanceId={m_InstanceId}"
            );
            return;
        }

        // ⭐ 检查是否已死亡
        if (instance.IsDead)
        {
            DebugEx.LogModule(
                "ChessItemUI",
                $"OnBeginDrag: 棋子已死亡，无法拖拽 instanceId={m_InstanceId}"
            );
            return;
        }

        // 检查是否已出战
        if (instance.IsDeployed)
        {
            DebugEx.LogModule(
                "ChessItemUI",
                $"OnBeginDrag: 棋子已出战，无法拖拽 instanceId={m_InstanceId}"
            );
            return;
        }

        m_IsDragging = true;

        // 设置半透明
        m_CanvasGroup.alpha = 0.6f;

        // ⭐ 触发拖拽开始回调（由 CombatPreparationUI 处理放置逻辑）
        m_OnDragBeginCallback?.Invoke(m_InstanceId);

        DebugEx.LogModule("ChessItemUI", $"OnBeginDrag: instanceId={m_InstanceId}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 拖拽过程中不做处理，预览位置由 ChessPlacementManager 更新
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!m_IsDragging)
            return;

        m_IsDragging = false;

        // 恢复透明度
        m_CanvasGroup.alpha = 1f;

        // 触发拖拽结束回调
        m_OnDragEndCallback?.Invoke(m_InstanceId);

        DebugEx.LogModule("ChessItemUI", $"OnEndDrag: instanceId={m_InstanceId}");
    }

    #endregion
}
