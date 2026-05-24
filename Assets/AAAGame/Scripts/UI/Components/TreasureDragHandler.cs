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

    public void Initialize(
        int treasureInstanceId,
        bool isFromInventory,
        RectTransform sourceSlotRect
    )
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
        DebugEx.Log(
            nameof(TreasureDragHandler),
            $"[OnBeginDrag] 开始拖拽宝物: {m_TreasureInstanceId}, 来自: {(m_IsDraggingFromInventory ? "仓库" : "宝物槽")}"
        );

        CreateDragIcon();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (m_DragIcon != null && m_TopCanvas != null)
        {
            var canvasRT = m_TopCanvas.GetComponent<RectTransform>();
            if (
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRT,
                    eventData.position,
                    m_TopCanvas.worldCamera,
                    out var localPoint
                )
            )
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
        if (m_TopCanvas == null)
            return;

        var dragIconObj = new GameObject("TreasureDragIcon");
        dragIconObj.transform.SetParent(m_TopCanvas.transform, false);

        m_DragIcon = dragIconObj.AddComponent<Image>();

        // ⭐ 从 TreasureItemUI 获取宝物图标
        var treasureItemUI = GetComponentInChildren<TreasureItemUI>(true);
        if (treasureItemUI != null)
        {
            // 从 TreasureItemUI 的子对象中查找名为 "TreasureImg" 的 Image 组件
            var treasureImgTransform = treasureItemUI.transform.Find("TreasureImg");
            if (treasureImgTransform != null)
            {
                var treasureImg = treasureImgTransform.GetComponent<Image>();
                if (treasureImg != null && treasureImg.sprite != null)
                {
                    m_DragIcon.sprite = treasureImg.sprite;
                    m_DragIcon.color = new Color(1, 1, 1, 0.7f);
                    DebugEx.Log(
                        nameof(TreasureDragHandler),
                        "[CreateDragIcon] 从 TreasureItemUI 获取图标成功"
                    );
                }
            }
        }

        // 备用方案：从当前对象的 Image 组件获取
        if (m_DragIcon.sprite == null)
        {
            var sourceImage = GetComponent<Image>();
            if (sourceImage != null && sourceImage.sprite != null)
            {
                m_DragIcon.sprite = sourceImage.sprite;
                m_DragIcon.color = new Color(1, 1, 1, 0.7f);
                DebugEx.Warning(
                    nameof(TreasureDragHandler),
                    "[CreateDragIcon] 未找到 TreasureItemUI 图标，使用备用方案"
                );
            }
        }

        // ⭐ 修复：禁用拖拽图标的射线检测，避免阻挡目标槽位
        m_DragIcon.raycastTarget = false;

        var rectTransform = dragIconObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100, 100);

        DebugEx.Log(
            nameof(TreasureDragHandler),
            $"[CreateDragIcon] 创建拖拽图标，raycastTarget={m_DragIcon.raycastTarget}"
        );
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
            position = mousePosition,
        };

        // 使用所有 GraphicRaycaster 进行射线检测（不只是顶层Canvas）
        var results = new System.Collections.Generic.List<RaycastResult>();

        // 获取所有 GraphicRaycaster
        var allRaycasters = FindObjectsOfType<GraphicRaycaster>();

        DebugEx.Log(
            nameof(TreasureDragHandler),
            $"[GetTargetSlot] 找到 {allRaycasters.Length} 个 GraphicRaycaster"
        );

        foreach (var raycaster in allRaycasters)
        {
            var tempResults = new System.Collections.Generic.List<RaycastResult>();
            raycaster.Raycast(pointerEventData, tempResults);
            results.AddRange(tempResults);
        }

        DebugEx.Log(
            nameof(TreasureDragHandler),
            $"[GetTargetSlot] 射线检测到 {results.Count} 个对象"
        );

        // 遍历射线检测结果，查找带有TreasureSlotDropHandler组件的对象
        foreach (var result in results)
        {
            DebugEx.Log(
                nameof(TreasureDragHandler),
                $"[GetTargetSlot] 检测到对象: {result.gameObject.name}, Canvas={result.gameObject.GetComponentInParent<Canvas>()?.name}"
            );

            // ⭐ 通过组件检测而不是Tag
            var dropHandler = result.gameObject.GetComponent<TreasureSlotDropHandler>();
            if (dropHandler != null)
            {
                DebugEx.Success(
                    nameof(TreasureDragHandler),
                    $"[GetTargetSlot] 找到目标槽位（通过TreasureSlotDropHandler）: {result.gameObject.name}"
                );
                return result.gameObject.GetComponent<RectTransform>();
            }
        }

        DebugEx.Warning(
            nameof(TreasureDragHandler),
            "[GetTargetSlot] 未找到带有TreasureSlotDropHandler组件的对象"
        );

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
                    DebugEx.Success(
                        nameof(TreasureDragHandler),
                        $"拖拽装备宝物: {m_TreasureInstanceId} 到棋子 {chessId}"
                    );
                    characterBagUI.RefreshTreasureUI();
                }
            }
        }
        else
        {
            // 从宝物槽拖拽到仓库：卸装宝物
            treasureManager.UnequipTreasure(m_TreasureInstanceId);
            treasureManager.SaveCurrentSave();
            DebugEx.Success(nameof(TreasureDragHandler), $"拖拽卸装宝物: {m_TreasureInstanceId}");

            if (characterBagUI != null)
            {
                characterBagUI.RefreshTreasureUI();
            }
        }
    }
}
