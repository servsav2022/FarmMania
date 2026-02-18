using UnityEngine;

public class SellItemButton : MonoBehaviour
{
    [SerializeField] private InventoryWindowController inventoryWindow;
    [SerializeField] private ItemDatabase itemDatabase;

    [Min(1)]
    [SerializeField] private int amountToSell = 1;

    public void Sell()
    {
        if (inventoryWindow == null)
        {
            Debug.LogError("[Sell] InventoryWindow not assigned");
            return;
        }

        string itemId = inventoryWindow.SelectedItemId;

        if (string.IsNullOrEmpty(itemId))
        {
            Debug.Log("[Sell] No item selected");
            return;
        }

        var um = UserManager.Instance;
        if (um == null || um.CurrentUser == null)
        {
            Debug.Log("[Sell] No active user");
            return;
        }

        int have = um.GetItemAmount(itemId);
        if (have < amountToSell)
        {
            Debug.Log($"[Sell] Not enough {itemId}. Have {have}");
            return;
        }

        int price = 1;
        if (itemDatabase != null)
        {
            var data = itemDatabase.Get(itemId);
            if (data != null)
                price = data.basePrice;
        }

        if (!um.TryRemoveItemFromInventory(itemId, amountToSell))
        {
            Debug.Log("[Sell] Failed to remove item");
            return;
        }

        um.AddMoneyToCurrentUser(price * amountToSell);

        Debug.Log($"[Sell] Sold {itemId} x{amountToSell}");
        inventoryWindow.ForceRefresh();
    }
}