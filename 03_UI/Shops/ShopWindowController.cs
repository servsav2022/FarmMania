using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopWindowController : MonoBehaviour
{
    public static ShopWindowController Instance { get; private set; }

    [Header("Корень окна")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;

    [Header("Список магазина (верх)")]
    [SerializeField] private Transform shopListContent;

    [Header("Список инвентаря (низ)")]
    [SerializeField] private Transform inventoryListContent;

    [Header("Префабы")]
    [SerializeField] private InventoryListRowUI rowPrefab;

    [Header("Кнопки")]
    [SerializeField] private Button buyButton;
    [SerializeField] private Button sellButton;

    [Header("Данные")]
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("Экономика магазина")]
    [SerializeField] private ShopEconomySettings economySettings;

    [Header("Баланс: рост цен")]
    [SerializeField] private int seedBuyAddPerCycle = 5;
    [SerializeField] private int productSellAddPerCycle = 3;

    [Header("Баланс: защита от тупика")]
    [SerializeField] private int minProfitPerPlantMin = 2;
    [SerializeField] private int minProfitPerPlantMax = 25;

    private ShopData currentShop;
    private ItemData selectedShopItem;
    private InventoryItem selectedInventoryItem;

    private readonly Dictionary<string, ShopStockEntry> shopStock = new();

    // Минимально допустимая цена продажи продукта, рассчитанная по ценам семян и урожайности
    private readonly Dictionary<string, int> sellFloorByItemId = new(StringComparer.OrdinalIgnoreCase);

    [Serializable]
    private class ShopStockEntry
    {
        public string itemId;
        public int amount;
        public int buyPrice;
        public int sellPrice;
    }

    private void Awake()
    {
        Instance = this;

        if (root != null)
            root.SetActive(false);
    }

    // ================= ОТКРЫТИЕ / ЗАКРЫТИЕ =================

    public void Open(ShopData shop)
    {
        currentShop = shop;
        selectedShopItem = null;
        selectedInventoryItem = null;

        BuildShopStock();

        if (titleText != null)
            titleText.text = shop != null ? shop.displayName : "Магазин";

        if (root != null)
            root.SetActive(true);

        Refresh();
    }

    public void Close()
    {
        currentShop = null;
        selectedShopItem = null;
        selectedInventoryItem = null;
        shopStock.Clear();
        sellFloorByItemId.Clear();

        if (root != null)
            root.SetActive(false);
    }

    // ================= ДЕЙСТВИЯ =================

    public void BuySelected()
    {
        if (selectedShopItem == null)
            return;

        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[SHOP][BUY] GameManager.Instance == null");
            return;
        }

        string itemId = selectedShopItem.itemId;

        if (!shopStock.TryGetValue(Normalize(itemId), out var stock) || stock.amount <= 0)
        {
            Debug.LogWarning($"[SHOP][BUY] Нет товара на складе: {itemId}");
            return;
        }

        int price = Mathf.Max(0, stock.buyPrice);

        if (!gm.TrySpendMoney(price))
        {
            Debug.Log($"[SHOP][BUY] Недостаточно денег. Нужно: {price}");
            return;
        }

        gm.AddItem(itemId, 1);
        stock.amount = Mathf.Max(0, stock.amount - 1);

        if (AudioManager.I != null)
            AudioManager.I.PlayBuy();

        if (QuestManagerMono.Instance != null)
            QuestManagerMono.Instance.ReportTag(QuestTags.ItemBought, 1);

        Refresh();
    }

    public void SellSelected()
    {
        if (selectedInventoryItem == null)
            return;

        var um = UserManager.Instance;
        var gm = GameManager.Instance;
        if (um == null || gm == null)
            return;

        string itemId = selectedInventoryItem.itemId;

        int sellPrice = 0;
        if (itemDatabase != null)
        {
            var data = itemDatabase.Get(itemId);
            sellPrice = GetSellPriceForItem(data);
        }

        if (!um.TryRemoveItemFromInventory(itemId, 1))
        {
            Debug.LogWarning($"[SHOP][SELL] Не удалось убрать предмет из инвентаря: {itemId}");
            return;
        }

        gm.AddMoney(sellPrice);

        if (AudioManager.I != null)
            AudioManager.I.PlaySell();

        if (QuestManagerMono.Instance != null)
            QuestManagerMono.Instance.ReportTag(QuestTags.ItemSold, 1);

        Refresh();
    }

    // ================= ОБНОВЛЕНИЕ ОКНА =================

    private void Refresh()
    {
        RefreshShopList();
        RefreshInventoryList();
        RefreshButtons();
    }

    // ================= СПИСОК МАГАЗИНА (ВЕРХ) =================

    private void RefreshShopList()
    {
        Clear(shopListContent);

        if (currentShop == null || currentShop.itemsForSale == null)
            return;

        foreach (var item in currentShop.itemsForSale)
        {
            if (item == null)
                continue;

            string itemId = item.itemId;

            string displayName = !string.IsNullOrEmpty(item.itemName)
                ? item.itemName
                : (itemDatabase != null ? itemDatabase.GetDisplayName(itemId) : itemId);

            shopStock.TryGetValue(Normalize(itemId), out var stock);

            int amount = stock != null ? stock.amount : 0;
            int price = stock != null ? stock.buyPrice : 0;

            bool selected =
                selectedShopItem != null &&
                Normalize(selectedShopItem.itemId) == Normalize(itemId);

            var row = Instantiate(rowPrefab, shopListContent);
            row.Bind(
                itemId,
                displayName,
                amount,
                price,
                selected,
                _ =>
                {
                    selectedShopItem = item;
                    selectedInventoryItem = null;
                    Refresh();
                }
            );
        }
    }

    // ================= ИНВЕНТАРЬ (НИЗ) =================

    private void RefreshInventoryList()
    {
        Clear(inventoryListContent);

        var save = UserManager.Instance?.CurrentSave;
        if (save == null || save.inventory == null)
            return;

        foreach (var slot in save.inventory)
        {
            if (slot.amount <= 0)
                continue;

            string displayName =
                itemDatabase != null
                    ? itemDatabase.GetDisplayName(slot.itemId)
                    : slot.itemId;

            bool selected = ReferenceEquals(slot, selectedInventoryItem);

            int sellPrice = 0;
            if (itemDatabase != null)
            {
                var itemData = itemDatabase.Get(slot.itemId);
                sellPrice = GetSellPriceForItem(itemData);
            }

            var row = Instantiate(rowPrefab, inventoryListContent);
            row.Bind(
                slot.itemId,
                displayName,
                slot.amount,
                sellPrice,
                selected,
                _ =>
                {
                    selectedInventoryItem = slot;
                    selectedShopItem = null;
                    Refresh();
                }
            );
        }
    }

    private void RefreshButtons()
    {
        if (buyButton != null)
        {
            bool canBuy = false;

            if (selectedShopItem != null &&
                shopStock.TryGetValue(Normalize(selectedShopItem.itemId), out var stock) &&
                stock.amount > 0)
            {
                canBuy = true;
            }

            buyButton.interactable = canBuy;
        }

        if (sellButton != null)
        {
            bool canSell = selectedInventoryItem != null && selectedInventoryItem.amount > 0;
            sellButton.interactable = canSell;
        }
    }

    // ================= СКЛАД / ЭКОНОМИКА =================

    private void BuildShopStock()
    {
        shopStock.Clear();
        sellFloorByItemId.Clear();

        if (currentShop == null || currentShop.itemsForSale == null)
            return;

        foreach (var item in currentShop.itemsForSale)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.itemId))
                continue;

            int amount = economySettings != null
                ? economySettings.RollStock()
                : 1;

            int buyPrice = GetBuyPriceForShopItem(item);
            int sellPrice = GetSellPriceForItem(item);

            shopStock[Normalize(item.itemId)] = new ShopStockEntry
            {
                itemId = item.itemId,
                amount = amount,
                buyPrice = buyPrice,
                sellPrice = sellPrice
            };

            AccumulateSellFloorFromSeed(item, buyPrice);
        }

        Debug.Log($"[SHOP][STOCK] Склад сформирован. Позиций: {shopStock.Count}");
    }

    private int GetQuestCycle()
    {
        return Mathf.Max(0, UserManager.Instance?.CurrentSave?.questCycle ?? 0);
    }

    private int GetBuyPriceForShopItem(ItemData item)
    {
        if (item == null)
            return 0;

        int basePrice = Mathf.Max(0, item.basePrice);

        int price = economySettings != null
            ? economySettings.GetBuyPrice(basePrice)
            : Mathf.Max(1, basePrice);

        if (item.itemType == ItemData.ItemType.Seed)
        {
            int cycle = GetQuestCycle();
            price += cycle * Mathf.Max(0, seedBuyAddPerCycle);
        }

        return Mathf.Max(1, price);
    }

    private int GetSellPriceForItem(ItemData item)
    {
        if (item == null)
            return 0;

        int basePrice = Mathf.Max(0, item.basePrice);

        int price = economySettings != null
            ? economySettings.GetSellPrice(basePrice)
            : Mathf.Max(0, basePrice);

        // Рост по кругам применяется только к продуктам
        if (item.itemType == ItemData.ItemType.Product)
        {
            int cycle = GetQuestCycle();
            price += cycle * Mathf.Max(0, productSellAddPerCycle);
        }

        // Применяем защитный порог, если он рассчитан
        if (sellFloorByItemId.TryGetValue(item.itemId, out int floor))
            price = Mathf.Max(price, floor);

        return Mathf.Max(0, price);
    }

    private void AccumulateSellFloorFromSeed(ItemData seedItem, int seedBuyPrice)
    {
        if (seedItem == null)
            return;

        if (seedItem is not SeedData seed)
            return;

        if (seed.cropToPlant == null)
            return;

        var product = seed.cropToPlant.harvestProduct;
        if (product == null)
            return;

        int harvestAmount = Mathf.Max(1, seed.cropToPlant.harvestAmount);

        int minProfit = Mathf.RoundToInt(seedBuyPrice * 0.25f);
        minProfit = Mathf.Clamp(minProfit, minProfitPerPlantMin, minProfitPerPlantMax);

        int requiredPerOne = Mathf.CeilToInt((seedBuyPrice + minProfit) / (float)harvestAmount);

        if (sellFloorByItemId.TryGetValue(product.itemId, out int cur))
            sellFloorByItemId[product.itemId] = Mathf.Max(cur, requiredPerOne);
        else
            sellFloorByItemId[product.itemId] = requiredPerOne;
    }

    // ================= ПОМОЩНИКИ ЧЕРЕЗ REFLECTION =================
    // Оставлено на случай, если в будущем потребуется расширить настройки экономики

    private int GetEconomyInt(string fieldName, int fallback, string altName = null)
    {
        if (economySettings == null)
            return fallback;

        try
        {
            var t = economySettings.GetType();

            var f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? (altName != null ? t.GetField(altName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : null);
            if (f != null)
                return Convert.ToInt32(f.GetValue(economySettings));

            var p = t.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? (altName != null ? t.GetProperty(altName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : null);
            if (p != null)
                return Convert.ToInt32(p.GetValue(economySettings));
        }
        catch
        {
            // Игнорируем ошибки и используем fallback
        }

        return fallback;
    }

    private float GetEconomyFloat(string fieldName, float fallback, string altName = null)
    {
        if (economySettings == null)
            return fallback;

        try
        {
            var t = economySettings.GetType();

            var f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? (altName != null ? t.GetField(altName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : null);
            if (f != null)
                return Convert.ToSingle(f.GetValue(economySettings));

            var p = t.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? (altName != null ? t.GetProperty(altName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : null);
            if (p != null)
                return Convert.ToSingle(p.GetValue(economySettings));
        }
        catch
        {
            // Игнорируем ошибки и используем fallback
        }

        return fallback;
    }

    // ================= ВСПОМОГАТЕЛЬНЫЕ =================

    private static string Normalize(string s) => (s ?? string.Empty).Trim().ToLowerInvariant();

    private static void Clear(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }
    }
}
