using UnityEngine;

[CreateAssetMenu(fileName = "NewProduct", menuName = "Farming Game/Product Data")]
public class ProductData : ItemData
{
    [Header("Product Settings")]
    [Tooltip("Цена продажи. Если 0 — используется Base Price из ItemData")]
    public int sellPrice = 0;

    public int SellPrice => sellPrice > 0 ? sellPrice : basePrice;

    private void OnValidate()
    {
        itemType = ItemType.Product; // чтобы не забыть руками
        if (maxStackSize <= 0) maxStackSize = 99;

        if (basePrice < 0) basePrice = 0;
        if (sellPrice < 0) sellPrice = 0;
    }
}