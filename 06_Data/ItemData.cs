using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Farming Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic Information")]
    public string itemId = "item_001";
    public string itemName = "New Item";
    public string description = "Item description";
    public Sprite icon;

    [Header("Item Settings")]
    public ItemType itemType = ItemType.Product;
    public int maxStackSize = 99;
    public int basePrice = 10;

    public enum ItemType
    {
        Seed,
        Product,
        Tool,
        Consumable
    }
}
