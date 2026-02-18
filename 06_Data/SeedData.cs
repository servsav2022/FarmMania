using UnityEngine;

[CreateAssetMenu(fileName = "NewSeed", menuName = "Farming Game/Seed Data")]
public class SeedData : ItemData
{
    [Header("Seed Settings")]
    public CropData cropToPlant;

    private void OnValidate()
    {
        itemType = ItemType.Seed; // 
        if (maxStackSize <= 0) maxStackSize = 99;
    }
}

