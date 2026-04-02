using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 背包/仓库物品拖拽处理器，挂在 InventorySlotUI 上。
/// 支持：背包内移动、背包→仓库存入、仓库→背包取出。
/// </summary>
public class InventoryDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    #region 字段

    private Image m_DragIcon;
    private InventorySlotUI m_SourceSlot;
    private Canvas m_TopCanvas;

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
            var rect = GetComponent<RectTransform>().rect;
            DebugEx.Log("DragHandler",
                $"[EnsureRaycastable] Image.raycastTarget={img.raycastTarget}, " +
                $"rect.size={rect.size}, color.a={img.color.a}, sprite={img.sprite}");
        }
    }

    #endregion

    #region 拖拽接口

    public void OnBeginDrag(PointerEventData eventData)
    {
        DebugEx.Log("DragHandler", $"[OnBeginDrag] 触发，GameObject={gameObject.name}，Tag={gameObject.tag}");

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

        DebugEx.Log("DragHandler", $"[OnBeginDrag] 找到源格子: 容器={m_SourceSlot.ContainerType} 格子={m_SourceSlot.SlotIndex}");

        var itemUI = m_SourceSlot.GetItemUI();
        if (itemUI == null)
        {
            DebugEx.Warning("DragHandler", $"[OnBeginDrag] ItemUI 为 null (SlotIndex={m_SourceSlot.SlotIndex})");
            m_SourceSlot = null;
            return;
        }

        if (!itemUI.HasItem())
        {
            DebugEx.Log("DragHandler", $"[OnBeginDrag] 格子为空 (SlotIndex={m_SourceSlot.SlotIndex})");
            m_SourceSlot = null;
            return;
        }

        DebugEx.Log("DragHandler", $"[OnBeginDrag] 开始拖拽: 容器={m_SourceSlot.ContainerType} 格子={m_SourceSlot.SlotIndex}");
        CreateDragIcon();
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
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        DebugEx.Log("DragHandler", $"[OnEndDrag] 拖拽结束，鼠标位置={eventData.position}");

        if (m_SourceSlot == null)
        {
            DebugEx.Warning("DragHandler", "[OnEndDrag] 源格子为 null，直接清理");
            CleanupDrag();
            return;
        }

        var targetSlot = GetTargetSlot(eventData.position);
        if (targetSlot == null)
        {
            DebugEx.Log("DragHandler", "[OnEndDrag] 目标格子为 null，拖拽取消");
        }
        else if (targetSlot == m_SourceSlot)
        {
            DebugEx.Log("DragHandler", "[OnEndDrag] 目标格子等于源格子，拖拽取消");
        }
        else
        {
            DebugEx.Log("DragHandler", $"[OnEndDrag] 执行拖放 源={m_SourceSlot.ContainerType}/{m_SourceSlot.SlotIndex} → 目标={targetSlot.ContainerType}/{targetSlot.SlotIndex}");
            HandleDrop(m_SourceSlot, targetSlot);
        }

        CleanupDrag();
    }

    #endregion

    #region 拖放逻辑

    private void HandleDrop(InventorySlotUI src, InventorySlotUI dst)
    {
        // 新架构：拖拽就是源格子所属容器对目标容器的一次交互
        if (src.SlotContainer == null || dst.SlotContainer == null)
        {
            DebugEx.Warning("DragHandler", "[HandleDrop] 源或目标容器为 null");
            return;
        }

        var srcType = src.ContainerType;
        var dstType = dst.ContainerType;
        var srcIndex = src.SlotIndex;
        var dstIndex = dst.SlotIndex;

        DebugEx.Log("DragHandler", $"[HandleDrop] 拖放: {srcType}/{srcIndex} → {dstType}/{dstIndex}");

        // 核心逻辑：调用源容器的交互方法
        bool success = src.SlotContainer.TryMoveToContainer(srcIndex, dst.SlotContainer, dstIndex);

        if (success)
        {
            DebugEx.Log("DragHandler", "[HandleDrop] 拖放成功");
            // 触发 UI 刷新事件或通知（后续由各容器的事件触发）
        }
        else
        {
            DebugEx.Warning("DragHandler", $"[HandleDrop] 拖放失败: {srcType} → {dstType}");
        }
    }

    #endregion

    #region 工具方法

    private void CreateDragIcon()
    {
        DebugEx.Log("DragHandler", "[CreateDragIcon] 开始创建拖拽图标");

        if (m_TopCanvas == null)
        {
            DebugEx.Warning("DragHandler", "[CreateDragIcon] TopCanvas 为 null");
            return;
        }

        var go = new GameObject("DragIcon");
        go.transform.SetParent(m_TopCanvas.transform, false);
        DebugEx.Log("DragHandler", "[CreateDragIcon] 创建 DragIcon GameObject");

        m_DragIcon = go.AddComponent<Image>();
        m_DragIcon.raycastTarget = false;
        DebugEx.Log("DragHandler", "[CreateDragIcon] 添加 Image 组件");

        // 配置 RectTransform：中心 Pivot，完全铺满（用于拖拽时跟随鼠标）
        var rt = m_DragIcon.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.one * 0.5f; // 中心点
        rt.sizeDelta = new Vector2(80, 80);
        rt.anchoredPosition = Vector2.zero;

        var itemUI = m_SourceSlot.GetItemUI();
        if (itemUI != null)
        {
            DebugEx.Log("DragHandler", "[CreateDragIcon] 获取源物品 UI");
            itemUI.TryGetComponent(out Image srcImg);
            if (srcImg == null)
            {
                DebugEx.Log("DragHandler", "[CreateDragIcon] TryGetComponent 未找到，尝试 GetComponentInChildren");
                srcImg = itemUI.GetComponentInChildren<Image>();
            }
            if (srcImg != null)
            {
                m_DragIcon.sprite = srcImg.sprite;
                m_DragIcon.color = new Color(1f, 1f, 1f, 0.7f); // 70% 透明度，更清晰
                var spriteName = srcImg.sprite != null ? srcImg.sprite.name : "null";
                DebugEx.Log("DragHandler", $"[CreateDragIcon] 设置图标 sprite={spriteName}");
            }
            else
            {
                DebugEx.Warning("DragHandler", "[CreateDragIcon] 无法找到源物品的 Image 组件");
            }
        }
        else
        {
            DebugEx.Warning("DragHandler", "[CreateDragIcon] ItemUI 为 null");
        }

        DebugEx.Log("DragHandler", "[CreateDragIcon] 拖拽图标创建完成");
    }

    private InventorySlotUI GetTargetSlot(Vector2 position)
    {
        DebugEx.Log("DragHandler", $"[GetTargetSlot] 开始射线检测，位置={position}");

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(
            new PointerEventData(EventSystem.current) { position = position },
            results
        );

        DebugEx.Log("DragHandler", $"[GetTargetSlot] 射线检测结果数量={results.Count}");

        foreach (var r in results)
        {
            DebugEx.Log("DragHandler", $"[GetTargetSlot] 检查对象: {r.gameObject.name}");
            var slot = r.gameObject.GetComponent<InventorySlotUI>();
            if (slot != null)
            {
                DebugEx.Log("DragHandler", $"[GetTargetSlot] 找到目标格子: 容器={slot.ContainerType} 格子={slot.SlotIndex}");
                return slot;
            }
        }

        DebugEx.Log("DragHandler", "[GetTargetSlot] 未找到有效的目标格子");
        return null;
    }

    private void CleanupDrag()
    {
        DebugEx.Log("DragHandler", "[CleanupDrag] 清理拖拽状态");

        if (m_DragIcon != null)
        {
            DebugEx.Log("DragHandler", "[CleanupDrag] 销毁 DragIcon GameObject");
            Destroy(m_DragIcon.gameObject);
            m_DragIcon = null;
        }
        else
        {
            DebugEx.Log("DragHandler", "[CleanupDrag] DragIcon 已为 null");
        }

        m_SourceSlot = null;
        DebugEx.Log("DragHandler", "[CleanupDrag] 清理完成");
    }

    private Canvas FindTopCanvas()
    {
        DebugEx.Log("DragHandler", "[FindTopCanvas] 开始查找最高层级的 Canvas");

        var canvases = FindObjectsOfType<Canvas>();
        DebugEx.Log("DragHandler", $"[FindTopCanvas] 找到 {canvases.Length} 个 Canvas");

        Canvas top = null;
        int maxOrder = int.MinValue;
        foreach (var c in canvases)
        {
            DebugEx.Log("DragHandler", $"[FindTopCanvas] Canvas: {c.gameObject.name} sortingOrder={c.sortingOrder}");
            if (c.sortingOrder > maxOrder)
            {
                maxOrder = c.sortingOrder;
                top = c;
            }
        }

        if (top != null)
        {
            DebugEx.Log("DragHandler", $"[FindTopCanvas] 选中最高层 Canvas: {top.gameObject.name} sortingOrder={top.sortingOrder}");
        }
        else
        {
            DebugEx.Warning("DragHandler", "[FindTopCanvas] 未找到有效的 Canvas");
        }

        return top;
    }

    #endregion
}
