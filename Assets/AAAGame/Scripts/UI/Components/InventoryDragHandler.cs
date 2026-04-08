using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 背包/仓库物品拖拽处理器，挂在 InventorySlotUI 上。
/// 支持：背包内移动、背包→仓库存入、仓库→背包取出、装备拖拽到棋子。
/// </summary>
public class InventoryDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    #region 常量

    /// <summary>装备检测范围（世界空间距离）</summary>
    private const float EQUIP_DETECT_RADIUS = 3f;

    #endregion

    #region 字段

    private Image m_DragIcon;
    private InventorySlotUI m_SourceSlot;
    private Canvas m_TopCanvas;

    // 装备→棋子拖拽
    private bool m_IsDraggingEquipment;
    private ChessEntity m_HighlightedChess;

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        m_TopCanvas = FindTopCanvas();
        EnsureRaycastable();
    }

    /// <summary>
    /// 确保当前物体能接收拖拽事件
    /// </summary>
    private void EnsureRaycastable()
    {
        if (TryGetComponent(out Image img))
        {
            img.raycastTarget = true;
        }
    }

    #endregion

    #region 拖拽接口

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 兼容两种配置：
        // 1. InventoryDragHandler 在 InventorySlotUI 上 → GetComponent
        // 2. InventoryDragHandler 在 InventoryItemUI 上 → GetComponentInParent
        m_SourceSlot = GetComponent<InventorySlotUI>();
        if (m_SourceSlot == null)
        {
            m_SourceSlot = GetComponentInParent<InventorySlotUI>();
        }

        if (m_SourceSlot == null)
        {
            DebugEx.Warning("DragHandler", $"[OnBeginDrag] 无法找到 InventorySlotUI (GameObject={gameObject.name})");
            return;
        }

        // Chess 槽不允许拖拽
        if (m_SourceSlot.ContainerType == SlotContainerType.Chess)
        {
            m_SourceSlot = null;
            return;
        }

        var itemUI = m_SourceSlot.GetItemUI();
        if (itemUI == null || !itemUI.HasItem())
        {
            m_SourceSlot = null;
            return;
        }

        DebugEx.Log("DragHandler", $"[OnBeginDrag] 开始拖拽: 容器={m_SourceSlot.ContainerType} 格子={m_SourceSlot.SlotIndex}");
        CreateDragIcon();

        // 检测是否为装备物品（仅 Equip 容器支持拖拽到 3D 棋子）
        var item = itemUI.GetItemStack()?.Item;
        m_IsDraggingEquipment = item is EquipmentItem && m_SourceSlot.ContainerType == SlotContainerType.Equip;
        if (m_IsDraggingEquipment)
        {
            DebugEx.LogModule("DragHandler", $"拖拽装备: {item.Name}");
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (m_DragIcon != null)
        {
            // 将鼠标屏幕坐标转换为 Canvas 本地坐标
            var canvasRT = m_TopCanvas.GetComponent<RectTransform>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRT,
                eventData.position,
                m_TopCanvas.worldCamera,
                out var localPoint))
            {
                m_DragIcon.rectTransform.anchoredPosition = localPoint;
            }
        }

        // 装备拖拽：检测范围内最近的友方棋子
        if (m_IsDraggingEquipment)
        {
            UpdateEquipTargetHighlight(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (m_SourceSlot == null)
        {
            CleanupDrag();
            return;
        }

        // 装备拖拽到棋子
        if (m_IsDraggingEquipment && m_HighlightedChess != null)
        {
            TryEquipToHighlightedChess();
            CleanupDrag();
            return;
        }

        // 原有逻辑：拖放到 UI 格子
        var targetSlot = GetTargetSlot(eventData.position);
        if (targetSlot != null && targetSlot != m_SourceSlot)
        {
            DebugEx.Log("DragHandler", $"[OnEndDrag] 执行拖放 源={m_SourceSlot.ContainerType}/{m_SourceSlot.SlotIndex} → 目标={targetSlot.ContainerType}/{targetSlot.SlotIndex}");
            HandleDrop(m_SourceSlot, targetSlot);
        }

        CleanupDrag();
    }

    #endregion

    #region 装备→棋子拖拽

    /// <summary>
    /// 更新装备拖拽时的棋子高亮
    /// </summary>
    private void UpdateEquipTargetHighlight(Vector2 screenPos)
    {
        Vector3 worldPos = GetWorldPosFromScreen(screenPos);

        // 无法获取世界坐标（鼠标不在战场上方）
        if (worldPos == Vector3.zero)
        {
            ClearHighlight();
            return;
        }

        var nearest = FindNearestFriendlyChess(worldPos);

        if (nearest == m_HighlightedChess)
            return; // 没变化

        // 切换高亮
        ClearHighlight();

        if (nearest != null)
        {
            m_HighlightedChess = nearest;
            if (m_HighlightedChess.OutlineController != null)
            {
                m_HighlightedChess.OutlineController.ShowOutline(
                    OutlineController.AllyColor,
                    OutlineController.DefaultSize);
            }
        }
    }

    /// <summary>
    /// 清除当前高亮棋子的描边
    /// </summary>
    private void ClearHighlight()
    {
        if (m_HighlightedChess != null)
        {
            if (m_HighlightedChess.OutlineController != null)
            {
                m_HighlightedChess.OutlineController.HideOutline();
            }
            m_HighlightedChess = null;
        }
    }

    /// <summary>
    /// 尝试将装备穿戴到高亮的棋子上
    /// </summary>
    private void TryEquipToHighlightedChess()
    {
        var itemUI = m_SourceSlot.GetItemUI();
        if (itemUI == null) return;

        var equipItem = itemUI.GetItemStack()?.Item as EquipmentItem;
        if (equipItem == null) return;

        int chessId = m_HighlightedChess.ChessId;
        var equipMgr = ChessEquipmentManager.Instance;
        int slotIndex = equipMgr.GetFirstEmptySlot(chessId);

        if (slotIndex < 0)
        {
            DebugEx.WarningModule("DragHandler", $"棋子 {chessId} 装备槽已满");
            return;
        }

        // 穿戴装备
        equipMgr.EquipItem(chessId, equipItem, slotIndex);

        // 从背包移除
        InventoryManager.Instance.RemoveItem(equipItem.ItemId, 1);

        DebugEx.LogModule("DragHandler", $"装备 {equipItem.Name} → 棋子 {chessId} 槽位 {slotIndex}");
    }

    /// <summary>
    /// 查找范围内最近的友方棋子（有空装备槽）
    /// </summary>
    private ChessEntity FindNearestFriendlyChess(Vector3 worldPos)
    {
        var summonMgr = SummonChessManager.Instance;
        if (summonMgr == null) return null;

        var allChess = summonMgr.GetAllChess();
        if (allChess == null) return null;

        var equipMgr = ChessEquipmentManager.Instance;
        ChessEntity nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var chess in allChess)
        {
            if (chess == null) continue;
            if (chess.Camp != (int)CampType.Player) continue;

            // 跳过装备槽满的棋子
            if (equipMgr.GetFirstEmptySlot(chess.ChessId) < 0) continue;

            float dist = Vector3.Distance(chess.transform.position, worldPos);
            if (dist <= EQUIP_DETECT_RADIUS && dist < nearestDist)
            {
                nearestDist = dist;
                nearest = chess;
            }
        }

        return nearest;
    }

    /// <summary>
    /// 屏幕坐标转世界坐标（参考 CardSlotItem.GetWorldPosFromScreen）
    /// </summary>
    private Vector3 GetWorldPosFromScreen(Vector2 screenPos)
    {
        var cam = Camera.main;
        if (cam == null) return Vector3.zero;

        Ray ray = cam.ScreenPointToRay(screenPos);

        // 优先用物理射线检测地面
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.point;
        }

        // 降级：假设 Y=0 平面
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.origin + ray.direction * distance;
        }

        return Vector3.zero;
    }

    #endregion

    #region 拖放逻辑

    private void HandleDrop(InventorySlotUI src, InventorySlotUI dst)
    {
        if (src.SlotContainer == null || dst.SlotContainer == null)
        {
            DebugEx.Warning("DragHandler", "[HandleDrop] 源或目标容器为 null");
            return;
        }

        DebugEx.Log("DragHandler", $"[HandleDrop] 拖放: {src.ContainerType}/{src.SlotIndex} → {dst.ContainerType}/{dst.SlotIndex}");

        bool success = src.SlotContainer.TryMoveToContainer(src.SlotIndex, dst.SlotContainer, dst.SlotIndex);

        if (success)
        {
            DebugEx.Log("DragHandler", "[HandleDrop] 拖放成功");
        }
        else
        {
            DebugEx.Warning("DragHandler", $"[HandleDrop] 拖放失败: {src.ContainerType} → {dst.ContainerType}");
        }
    }

    #endregion

    #region 工具方法

    private void CreateDragIcon()
    {
        if (m_TopCanvas == null) return;

        var go = new GameObject("DragIcon");
        go.transform.SetParent(m_TopCanvas.transform, false);

        m_DragIcon = go.AddComponent<Image>();
        m_DragIcon.raycastTarget = false;

        var rt = m_DragIcon.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = Vector2.one * 0.5f;
        rt.sizeDelta = new Vector2(80, 80);
        rt.anchoredPosition = Vector2.zero;

        var itemUI = m_SourceSlot.GetItemUI();
        if (itemUI != null)
        {
            itemUI.TryGetComponent(out Image srcImg);
            if (srcImg == null)
            {
                srcImg = itemUI.GetComponentInChildren<Image>();
            }
            if (srcImg != null)
            {
                m_DragIcon.sprite = srcImg.sprite;
                m_DragIcon.color = new Color(1f, 1f, 1f, 0.7f);
            }
        }
    }

    private InventorySlotUI GetTargetSlot(Vector2 position)
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(
            new PointerEventData(EventSystem.current) { position = position },
            results
        );

        foreach (var r in results)
        {
            var slot = r.gameObject.GetComponent<InventorySlotUI>();
            if (slot != null)
            {
                DebugEx.Log("DragHandler", $"[GetTargetSlot] 找到目标格子: 容器={slot.ContainerType} 格子={slot.SlotIndex}");
                return slot;
            }
        }

        return null;
    }

    private void CleanupDrag()
    {
        // 清除装备高亮
        ClearHighlight();
        m_IsDraggingEquipment = false;

        if (m_DragIcon != null)
        {
            Destroy(m_DragIcon.gameObject);
            m_DragIcon = null;
        }

        m_SourceSlot = null;
    }

    private Canvas FindTopCanvas()
    {
        var canvases = FindObjectsOfType<Canvas>();
        Canvas top = null;
        int maxOrder = int.MinValue;
        foreach (var c in canvases)
        {
            if (c.sortingOrder > maxOrder)
            {
                maxOrder = c.sortingOrder;
                top = c;
            }
        }
        return top;
    }

    #endregion
}
