using UnityEngine;

[CreateAssetMenu(menuName = "FarmMania/Shop/Shop Economy Settings", fileName = "ShopEconomySettings")]
public class ShopEconomySettings : ScriptableObject
{
    [Header("Stock per shop open")]
    public int minStock = 3;
    public int maxStock = 15;

    [Header("Prices")]
    [Range(0.1f, 5f)] public float buyMultiplier = 1.0f;
    [Range(0.0f, 5f)] public float sellMultiplier = 0.5f;

    public int RollStock()
    {
        if (maxStock < minStock) maxStock = minStock;
        return Random.Range(minStock, maxStock + 1);
    }

    public int GetBuyPrice(int basePrice)
        => Mathf.Max(1, Mathf.RoundToInt(basePrice * buyMultiplier));

    public int GetSellPrice(int basePrice)
        => Mathf.Max(0, Mathf.RoundToInt(basePrice * sellMultiplier));
}