using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Farm/Crop")]
public class CropData : ScriptableObject
{
    public string cropName;

    [Header("Visual")]
    public TileBase[] growthStages;

    [Header("Growth")]
    [Tooltip("Время в секундах на одну стадию роста. Если 0 или меньше, используется значение по умолчанию в FarmGridController.")]
    public float secondsPerStage = 5f;

    [Header("Harvest")]
    public ItemData harvestProduct;
    public int harvestAmount = 1;
}