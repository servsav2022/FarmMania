using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ShopData",
    menuName = "Game/Shop Data"
)]
public class ShopData : ScriptableObject
{
    [Header("Shop info")]
    [Tooltip("Уникальный идентификатор магазина")]
    public string shopId;

    [Tooltip("Отображаемое название магазина")]
    public string displayName;

    [Header("Assortment")]
    [Tooltip("Список предметов, доступных для покупки")]
    public List<ItemData> itemsForSale = new();
    
}