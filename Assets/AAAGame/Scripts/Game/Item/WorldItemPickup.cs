using UnityEngine;

// WorldItemPickup：挂在场景掉落物品预制体上，需要开启 Physics.queriesHitTriggers 或使用 Collider
/// <summary>
/// 场景掉落物品交互组件
/// 挂在场景物品预制体上，左键拾取，右键直接使用
/// </summary>
public class WorldItemPickup : MonoBehaviour
{
    [SerializeField] private int m_ItemId;
    [SerializeField] private int m_Count = 1;

    private void OnMouseEnter()
    {
        ShowTooltip();
    }

    private void OnMouseExit()
    {
        HideTooltip();
    }

    private void OnMouseDown()
    {
        var inputManager = PlayerInputManager.Instance;
        if (inputManager == null)
            return;

        // 右键：直接使用（可使用物品）
        if (inputManager.RightMouseButtonDown)
        {
            TryUseDirectly();
            return;
        }

        // 左键：拾取到背包
        if (inputManager.LeftMouseButtonDown)
        {
            TryPickup();
        }
    }

    private void TryPickup()
    {
        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
            return;

        bool success = inventoryManager.AddItem(m_ItemId, m_Count);
        if (success)
        {
            DebugEx.Log("WorldItemPickup", $"拾取物品 ID={m_ItemId} x{m_Count}");

            // 获取物品稀有度并给予对应经验
            var itemData = ItemManager.Instance?.GetItemData(m_ItemId);
            if (itemData != null)
                PlayerExpManager.Instance.GainExpFromItem((int)itemData.Quality);

            Destroy(gameObject);
        }
        else
        {
            DebugEx.Warning("WorldItemPickup", "背包已满，无法拾取");
            ShowFullTip();
        }
    }

    private void TryUseDirectly()
    {
        var itemData = ItemManager.Instance?.GetItemData(m_ItemId);
        if (itemData == null || !itemData.CanUse)
            return;

        // 创建临时物品实例并使用
        var item = ItemManager.Instance.CreateItem(m_ItemId);
        if (item == null)
            return;

        bool used = item.Use();
        if (used)
        {
            DebugEx.Log("WorldItemPickup", $"直接使用物品 ID={m_ItemId}");
            m_Count--;
            if (m_Count <= 0)
                Destroy(gameObject);
        }
    }

    private void ShowTooltip()
    {
        var itemData = ItemManager.Instance?.GetItemData(m_ItemId);
        if (itemData == null)
            return;

        // TODO: 接入项目 Tooltip 系统后替换
        DebugEx.Log("WorldItemPickup", $"[Tooltip] {itemData.Name}: {itemData.Description}");
    }

    private void HideTooltip()
    {
        // TODO: 隐藏 Tooltip
    }

    private void ShowFullTip()
    {
        DebugEx.Warning("WorldItemPickup", "背包已满，无法拾取");
    }
}
