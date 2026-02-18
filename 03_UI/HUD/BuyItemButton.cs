using UnityEngine;

public class BuyItemButton : MonoBehaviour
{
    [Header("Buy settings")]
    [SerializeField] private ItemDatabase itemDatabase;  // ItemDatabase.asset
    [SerializeField] private string itemIdToBuy = "seed_carrot";
    [SerializeField] private int amountToBuy = 1;
    [SerializeField] private InventoryWindowController inventoryWindow;
    
    public void Buy()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[Shop] GameManager not found.");
            return;
        }

        if (UserManager.Instance == null || UserManager.Instance.CurrentUser == null)
        {
            Debug.LogWarning("[Shop] No logged user.");
            return;
        }

        if (itemDatabase == null)
        {
            Debug.LogWarning("[Shop] ItemDatabase not assigned.");
            return;
        }

        var item = itemDatabase.Get(itemIdToBuy);
        if (item == null)
        {
            Debug.LogWarning($"[Shop] Item '{itemIdToBuy}' not found in ItemDatabase.");
            return;
        }

        int price = Mathf.Max(0, item.basePrice) * Mathf.Max(1, amountToBuy);

        if (!GameManager.Instance.TrySpendMoney(price))
        {
            Debug.Log($"[Shop] Not enough money to buy {item.itemName}. Need {price}, have {GameManager.Instance.Money}");
            return;
        }

        GameManager.Instance.AddItem(item.itemId, amountToBuy);
        Debug.Log($"[Shop] Bought {item.itemName} x{amountToBuy} for {price}. Balance: {GameManager.Instance.Money}");
        inventoryWindow.ForceRefresh();
    }
    
}