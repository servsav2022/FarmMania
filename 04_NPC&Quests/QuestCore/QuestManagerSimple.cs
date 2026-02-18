using System;
using System.Collections.Generic;

public class QuestManagerSimple
{
    private readonly Dictionary<string, QuestProgress> quests = new();

    // Текущий активный квест (по нему идет прогресс)
    public string ActiveQuestId { get; private set; }

    // Номер круга (0 = первый круг)
    public int Cycle { get; private set; }

    // Список выполненных квестов (по id)
    private readonly HashSet<string> completedIds = new();

    // События (Mono-обертка подпишется и покажет UI/сохранит прогресс)
    public event Action<QuestDefinition> QuestCompleted;
    public event Action AllQuestsCompleted;
    public event Action ActiveQuestChanged;

    public void AddQuest(QuestDefinition def)
    {
        if (def == null || string.IsNullOrWhiteSpace(def.id))
            return;

        string id = def.id.Trim();

        // Если baseTarget не задан — считаем его равным target (чтобы не ломать старые данные)
        if (def.baseTarget <= 0)
            def.baseTarget = Math.Max(0, def.target);

        // Актуализируем target под текущий круг
        def.target = CalcTarget(def, Cycle);

        quests[id] = new QuestProgress
        {
            def = def,
            current = 0,
            completed = false
        };
    }

    public QuestProgress Get(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        quests.TryGetValue(id.Trim(), out var q);
        return q;
    }

    public QuestProgress GetActive()
    {
        return Get(ActiveQuestId);
    }

    // Старый вариант (чтобы не ломать места, где вызов был без resetProgress)
    public void SetActive(string id)
    {
        SetActive(id, resetProgress: false);
    }

    // Новый вариант — для твоей ошибки CS1739 (resetProgress теперь реально существует)
    public void SetActive(string id, bool resetProgress)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            ActiveQuestId = null;
            ActiveQuestChanged?.Invoke();
            return;
        }

        id = id.Trim();

        if (!quests.ContainsKey(id))
        {
            ActiveQuestId = null;
            ActiveQuestChanged?.Invoke();
            return;
        }

        ActiveQuestId = id;

        if (resetProgress)
        {
            var q = quests[id];
            if (q != null)
            {
                q.current = 0;
                q.completed = false;
                completedIds.Remove(id);
            }
        }

        ActiveQuestChanged?.Invoke();
    }

    // Универсальный прогресс по тегу — только для активного квеста
    public void AdvanceActiveByTag(string requiredTag, int delta)
    {
        if (string.IsNullOrWhiteSpace(requiredTag) || delta == 0)
            return;

        var q = GetActive();
        if (q == null || q.def == null || q.completed)
            return;

        if (!string.Equals(q.def.targetTag, requiredTag, StringComparison.OrdinalIgnoreCase))
            return;

        int target = Math.Max(0, q.def.target);

        if (target == 0)
        {
            CompleteQuest(q);
            return;
        }

        q.current = Clamp(q.current + delta, 0, target);

        if (q.current >= target)
            CompleteQuest(q);
    }
    // Универсальный прогресс по тегу - для всех квестов (нужен для EditMode тестов
// и для ситуаций, когда активный квест еще не назначен).
    public void AdvanceAllByTag(string requiredTag, int delta)
    {
        if (string.IsNullOrWhiteSpace(requiredTag) || delta == 0)
            return;

        // Итерируем по снимку ключей, т.к. в процессе может вызываться CompleteQuest()
        // (он меняет ActiveQuestId и дергает события).
        var keys = new List<string>(quests.Keys);

        for (int i = 0; i < keys.Count; i++)
        {
            if (!quests.TryGetValue(keys[i], out var q))
                continue;
            if (q == null || q.def == null || q.completed)
                continue;

            if (!string.Equals(q.def.targetTag, requiredTag, StringComparison.OrdinalIgnoreCase))
                continue;

            int target = Math.Max(0, q.def.target);

            if (target == 0)
            {
                CompleteQuest(q);
                continue;
            }

            q.current = Clamp(q.current + delta, 0, target);

            if (q.current >= target)
                CompleteQuest(q);
        }
    }


    // Совместимость со старыми тестами/вызовами
    public void OnPlantedSeed()
    {
        // Если активный квест выбран, прогресс идет только по нему.
        // Если активный не выбран (например, в EditMode тестах), прогресс начисляется
        // всем подходящим квестам.
        if (!string.IsNullOrWhiteSpace(ActiveQuestId))
            AdvanceActiveByTag(QuestTags.SeedPlanted, 1);
        else
            AdvanceAllByTag(QuestTags.SeedPlanted, 1);
    }

    public void OnEarnedMoney(int amount)
    {
        if (amount <= 0) return;

        if (!string.IsNullOrWhiteSpace(ActiveQuestId))
            AdvanceActiveByTag(QuestTags.MoneyEarned, amount);
        else
            AdvanceAllByTag(QuestTags.MoneyEarned, amount);
    }


    private void CompleteQuest(QuestProgress q)
    {
        if (q == null || q.def == null || q.completed)
            return;

        q.completed = true;
        completedIds.Add(q.def.id);

        QuestCompleted?.Invoke(q.def);

        // Переход к следующему в цепочке (если задан)
        string nextId = q.def.nextQuestId;
        if (!string.IsNullOrWhiteSpace(nextId))
        {
            SetActive(nextId.Trim(), resetProgress: true);
            return;
        }

        // Если nextQuestId не задан — считаем, что цепочка закончилась
        ActiveQuestId = null;
        ActiveQuestChanged?.Invoke();

        // Проверяем "все ли выполнены"
        if (IsAllCompleted())
            AllQuestsCompleted?.Invoke();
    }

    public bool IsAllCompleted()
    {
        foreach (var kv in quests)
        {
            if (kv.Value == null) continue;
            if (!kv.Value.completed) return false;
        }
        return quests.Count > 0;
    }

    // Запуск нового круга с увеличением целей
    public void StartNewCycle(string startQuestId)
    {
        Cycle = Math.Max(0, Cycle + 1);

        completedIds.Clear();

        foreach (var kv in quests)
        {
            var q = kv.Value;
            if (q == null || q.def == null) continue;

            q.current = 0;
            q.completed = false;

            // Пересчитываем target под новый круг
            q.def.target = CalcTarget(q.def, Cycle);
        }

        SetActive(startQuestId, resetProgress: true);
    }

    // Сохранение для UserManager
    public void GetSaveState(out int cycle, out string activeId, out int activeProgress, out List<string> completed)
    {
        cycle = Cycle;
        activeId = ActiveQuestId;

        var active = GetActive();
        activeProgress = active != null ? active.current : 0;

        completed = new List<string>(completedIds);
    }

    // Восстановление для UserManager
    public void RestoreState(int cycle, string activeQuestId, int activeProgress, List<string> completedQuestIds)
    {
        Cycle = Math.Max(0, cycle);

        completedIds.Clear();
        if (completedQuestIds != null)
        {
            for (int i = 0; i < completedQuestIds.Count; i++)
            {
                var id = completedQuestIds[i];
                if (!string.IsNullOrWhiteSpace(id))
                    completedIds.Add(id.Trim());
            }
        }

        // Сначала сбрасываем все квесты и пересчитываем target под сохраненный круг
        foreach (var kv in quests)
        {
            var q = kv.Value;
            if (q == null || q.def == null) continue;

            q.current = 0;
            q.completed = completedIds.Contains(q.def.id);

            q.def.target = CalcTarget(q.def, Cycle);
        }

        // Активный квест
        if (!string.IsNullOrWhiteSpace(activeQuestId))
        {
            activeQuestId = activeQuestId.Trim();

            if (quests.TryGetValue(activeQuestId, out var active))
            {
                ActiveQuestId = activeQuestId;

                // Важно: активный не должен быть помечен completed, даже если кто-то сохранил так
                active.completed = false;
                completedIds.Remove(activeQuestId);

                int target = Math.Max(0, active.def.target);
                active.current = Clamp(activeProgress, 0, target);
            }
            else
            {
                ActiveQuestId = null;
            }
        }
        else
        {
            ActiveQuestId = null;
        }

        ActiveQuestChanged?.Invoke();
    }

    private int CalcTarget(QuestDefinition def, int cycle)
    {
        int baseT = Math.Max(0, def.baseTarget);
        int add = Math.Max(0, def.addPerCycle);
        long t = (long)baseT + (long)add * (long)cycle;
        if (t > int.MaxValue) t = int.MaxValue;
        return (int)t;
    }

    private static int Clamp(int v, int min, int max)
    {
        if (v < min) return min;
        if (v > max) return max;
        return v;
    }
}
