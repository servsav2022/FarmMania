using System;

[Serializable]
public class QuestDefinition
{
    // Уникальный id задания (пример: "q_plant_1")
    public string id;

    // Текст задания для UI
    public string title;

    // Тег события, которое увеличивает прогресс (пример: "seed_planted")
    public string targetTag;

    // Текущая цель (её будем пересчитывать по кругам)
    public int target = 1;

    // Базовая цель для 0 круга. Если 0, будет взято значение target
    public int baseTarget = 0;

    // На сколько увеличивать цель при каждом новом круге
    public int addPerCycle = 0;

    // Следующее задание в цепочке (если пусто, цепочка заканчивается)
    public string nextQuestId;
}