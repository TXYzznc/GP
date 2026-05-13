using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 宝物拖拽处理器
/// 支持从宝物仓库拖拽宝物到宝物槽装备，或从宝物槽拖拽到仓库卸装
/// </summary>
public class TreasureDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Image m_DragIcon;
    private Canvas m_TopCanvas;
    private int m_TreasureInstanceId;
    private bool m_IsDraggingFromInventory; // true=从宝物仓库, false=从宝物槽
    private RectTransform m_SourceSlotRect;

    public void Initialize(int treasureInstanceId, bool isFromInventory, RectTransform sourceSlotRect)
    {
        m_TreasureInstanceId = treasureInstanceId;
        m_IsDraggingFromInventory = isFromInventory;
        m_SourceSlotRect = sourceSlotRect;
    }

    private void Awake()
    {
        m_TopCanvas = FindTopCanvas();
        EnsureRaycastable();
    }

    private Canvas FindTopCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Canvas topCanvas = null;
        int maxSortingOrder = int.MinValue;

        foreach (Canvas canvas in canvases)
        {
            if (canvas.sortingOrder > maxSortingOrder)
            {
                maxSortingOrder = canvas.sortingOrder;
                topCanvas = canvas;
            }
        }

        return topCanvas ?? GetComponentInParent<Canvas>();
    }

    private void EnsureRaycastable()
    {
        if (TryGetComponent(out Image img))
        {
            img.raycastTarget = true;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        DebugEx.Log(nameof(TreasureDragHandler),
            $"[OnBeginDrag] 开始拖拽宝物: {m_TreasureInstanceId}, 来自: {(m_IsDraggingFromInventory ? "仓库" : "宝物槽")}");

        CreateDragIcon();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (m_DragIcon != null && m_TopCanvas != null)
        {
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
        // 检测拖拽目标
        var targetSlot = GetTargetSlot(eventData.position);

        if (targetSlot != null)
        {
            HandleDrop(targetSlot);
        }
        else
        {
            DebugEx.Warning(nameof(TreasureDragHandler), "[OnEndDrag] 未找到有效的拖拽目标");
        }

        CleanupDrag();
    }

    private void CreateDragIcon()
    {
        if (m_TopCanvas == null) return;

        var dragIconObj = new GameObject("TreasureDragIcon");
        dragIconObj.transform.SetParent(m_TopCanvas.transform, false);

        m_DragIcon = dragIconObj.AddComponent<Image>();
        var sourceImage = GetComponent<Image>();
        if (sourceImage != null)
        {
            m_DragIcon.sprite = sourceImage.sprite;
            m_DragIcon.color = new Color(1, 1, 1, 0.7f);
        }

        var rectTransform = dragIconObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100, 100);
    }

    private void CleanupDrag()
    {
        if (m_DragIcon != null)
        {
            Destroy(m_DragIcon.gameObject);
        }
    }

    private RectTransform GetTargetSlot(Vector2 mousePosition)
    {
        // 创建射线检测数据
        var pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = mousePosition
        };

        // 使用 GraphicRaycaster 进行射线检测
        var results = new System.Collections.Generic.List<RaycastResult>();
        var graphicRaycaster = m_TopCanvas?.GetComponent<GraphicRaycaster>();

        if (graphicRaycaster != null)
        {
            graphicRaycaster.Raycast(pointerEventData, results);

            // 遍历射线检测结果，查找宝物槽位
            foreach (var result in results)
            {
                if (result.gameObject.CompareTag("TreasureSlot"))
                {
                    return result.gameObject.GetComponent<RectTransform>();
                }
            }
        }

        return null;
    }

    private void HandleDrop(RectTransform targetSlot)
    {
        var treasureManager = PlayerAccountDataManager.Instance;
        var characterBagUI = GetComponentInParent<CharacterBagUI>();

        if (m_IsDraggingFromInventory)
        {
            // 从仓库拖拽到宝物槽：装备宝物
            if (characterBagUI != null)
            {
                int chessId = characterBagUI.GetCurrentSelectedChessId();
                if (chessId > 0)
                {
                    treasureManager.EquipTreasure(m_TreasureInstanceId, chessId);
                    treasureManager.SaveCurrentSave();
                    DebugEx.Success(nameof(TreasureDragHandler),
                        $"拖拽装备宝物: {m_TreasureInstanceId} 到棋子 {chessId}");
                    characterBagUI.RefreshTreasureUI();
                }
            }
        }
        else
        {
            // 从宝物槽拖拽到仓库：卸装宝物
            treasureManager.UnequipTreasure(m_TreasureInstanceId);
            treasureManager.SaveCurrentSave();
            DebugEx.Success(nameof(TreasureDragHandler),
                $"拖拽卸装宝物: {m_TreasureInstanceId}");

            if (characterBagUI != null)
            {
                characterBagUI.RefreshTreasureUI();
            }
        }
    }
}
