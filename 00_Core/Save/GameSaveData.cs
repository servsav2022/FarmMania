using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public string saveId;
    public DateTime savedAt;

    // Экономика
    public int money;

    // Инвентарь
    public List<InventoryItem> inventory = new();

    // Мир (грядки)
    public List<PlotSaveData> plots = new();

    // Задания
    public int questCycle;
    public string activeQuestId;
    public int activeQuestProgress;
    public List<string> completedQuestIds = new List<string>();

    // Позиция игрока
    // Важно: hasPlayerPosition защищает старые сейвы от телепорта в (0,0)
    public bool hasPlayerPosition;
    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;

    public void AddToInventory(string itemId, int amount)
    {
        var slot = inventory.Find(s => s.itemId == itemId);

        if (slot != null)
        {
            slot.amount += amount;
        }
        else
        {
            inventory.Add(new InventoryItem
            {
                itemId = itemId,
                amount = amount
            });
        }
    }

    public void RemoveFromInventory(string itemId, int amount)
    {
        var slot = inventory.Find(s => s.itemId == itemId);
        if (slot == null) return;

        slot.amount -= amount;

        if (slot.amount <= 0)
            inventory.Remove(slot);
    }
}