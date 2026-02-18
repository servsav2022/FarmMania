// QuestManagerMono.cs
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class QuestManagerMono : MonoBehaviour
{
    public static QuestManagerMono Instance { get; private set; }
    
    [Header("Bonus NPC Quest")]
    [SerializeField] private float bonusQuestDurationSeconds = 120f; // по умолчанию 2 минуты

    [Header("Quests setup")]
    [SerializeField] private QuestDefinition[] initialQuests;
    [SerializeField] private string startQuestId = "q_plant_1";

    [Header("UI")]
    [SerializeField] private Component toastUI;

    public event Action QuestsChanged;
    public event Action SaveRequested;

    private readonly Dictionary<string, QuestProgress> _quests = new();
    private readonly List<string> _completedIds = new();

    private int _questCycle = 0;
    private string _activeQuestId = null;
    
    private float _bonusQuestTimer = 0f;
    private bool _bonusQuestTimerActive = false;

    // ================= БОНУС-КВЕСТ ОТ NPC =================

    private QuestProgress _bonusQuest;

    private bool HasActiveBonusQuest
        => _bonusQuest != null && _bonusQuest.def != null && !_bonusQuest.completed;

    private const string BonusQuestId = "q_bonus_npc";

    // Реальные itemId из твоих ассетов harvestProduct
    private const string CarrotItemId = "product_carrot";
    private const string PotatoItemId = "product_potato";

    private static string TagHarvestCarrot => $"harvest:{CarrotItemId}";
    private static string TagHarvestPotato => $"harvest:{PotatoItemId}";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildFromInspector();
        ApplyCycleToAllDefinitions();
        ApplyCompletedVisualState();

        if (!string.IsNullOrWhiteSpace(startQuestId))
        {
            if (string.IsNullOrWhiteSpace(_activeQuestId))
                SetActive(startQuestId, resetProgress: true);
        }
    }

    // ================= ПУБЛИЧНЫЕ СОБЫТИЯ ИЗ ИГРЫ =================

    public void ReportPlantedSeed()
    {
        AdvanceActiveByTag("seed_planted", 1);
    }

    public void ReportMoneyEarned(int amount)
    {
        if (amount <= 0) return;
        AdvanceActiveByTag("money_earned", amount);
    }

    public void ReportTag(string tag, int delta)
    {
        if (string.IsNullOrWhiteSpace(tag) || delta == 0)
            return;

        string t = tag.Trim().ToLowerInvariant();

        AdvanceBonusByTag(t, delta);
        AdvanceActiveByTag(t, delta);
    }

    // ================= БОНУС-КВЕСТ: ВЫДАЧА ОТ NPC =================

    public bool TryAcceptBonusQuestFromNpc()
    {
        if (HasActiveBonusQuest)
        {
            ShowToast("Жадничать не хорошо. Сначала выполни прошлое задание.");
            return false;
        }

        bool carrot = UnityEngine.Random.Range(0, 2) == 0;
        int need = UnityEngine.Random.Range(5, 16); // 5..15

        string cropName = carrot ? "моркови" : "картофеля";
        string tag = carrot ? TagHarvestCarrot : TagHarvestPotato;

        int rewardPerUnit = carrot ? 10 : 15;
        int reward = need * rewardPerUnit;

        var def = new QuestDefinition
        {
            id = BonusQuestId,
            title = $"Собери {need} {cropName}",
            targetTag = tag,
            baseTarget = need,
            target = need,
            addPerCycle = 0,
            nextQuestId = null
        };

        _bonusQuest = new QuestProgress
        {
            def = def,
            current = 0,
            completed = false
        };
        
        _bonusQuestTimer = bonusQuestDurationSeconds;
        _bonusQuestTimerActive = true;

        ShowToast($"Получен дополнительный квест: {def.title}. Награда: {reward} монет.");

        QuestsChanged?.Invoke();
        SaveRequested?.Invoke();

        return true;
    }

    private void AdvanceBonusByTag(string tag, int delta)
    {
        if (!HasActiveBonusQuest)
            return;

        var b = _bonusQuest;
        if (b == null || b.def == null)
            return;

        if (!string.Equals(Norm(b.def.targetTag), Norm(tag), StringComparison.OrdinalIgnoreCase))
            return;

        int target = Mathf.Max(0, b.def.baseTarget);

        if (target <= 0)
        {
            CompleteBonusQuest();
            return;
        }

        b.current = Mathf.Clamp(b.current + delta, 0, target);
        b.completed = b.current >= target;

        QuestsChanged?.Invoke();
        SaveRequested?.Invoke();

        if (b.completed)
            CompleteBonusQuest();
    }

    private void CompleteBonusQuest()
    {
        if (_bonusQuest == null || _bonusQuest.def == null)
            return;
        _bonusQuestTimerActive = false;

        bool carrot = string.Equals(Norm(_bonusQuest.def.targetTag), Norm(TagHarvestCarrot), StringComparison.OrdinalIgnoreCase);
        int need = Mathf.Max(0, _bonusQuest.def.baseTarget);

        int rewardPerUnit = carrot ? 10 : 15;
        int reward = need * rewardPerUnit;

        if (!TryAddMoneyToCurrentUser(reward))
            Debug.LogWarning("[QuestManagerMono] Не удалось начислить награду: UserManager не найден или метод AddMoneyToCurrentUser недоступен.");

        ShowToast($"Поздравляем! Бонус-квест выполнен. Награда: {reward} монет.");

        _bonusQuest = null;

        QuestsChanged?.Invoke();
        SaveRequested?.Invoke();
    }

    // ================= ДАННЫЕ ДЛЯ UI =================

    public QuestProgress GetActiveQuest()
    {
        if (string.IsNullOrWhiteSpace(_activeQuestId))
            return null;

        _quests.TryGetValue(Norm(_activeQuestId), out var q);
        return q;
    }

    public QuestProgress GetQuest(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        if (_bonusQuest != null && _bonusQuest.def != null &&
            string.Equals(Norm(_bonusQuest.def.id), Norm(id), StringComparison.OrdinalIgnoreCase))
            return _bonusQuest;

        _quests.TryGetValue(Norm(id), out var q);
        return q;
    }

    public QuestProgress[] GetQuestsInOrder()
    {
        int baseCount = (initialQuests == null) ? 0 : initialQuests.Length;
        bool hasBonus = (_bonusQuest != null && _bonusQuest.def != null);

        if (baseCount == 0 && !hasBonus)
            return Array.Empty<QuestProgress>();

        var result = new List<QuestProgress>(baseCount + (hasBonus ? 1 : 0));

        if (hasBonus)
            result.Add(_bonusQuest);

        if (initialQuests != null)
        {
            for (int i = 0; i < initialQuests.Length; i++)
            {
                var def = initialQuests[i];
                if (def == null || string.IsNullOrWhiteSpace(def.id))
                    continue;

                result.Add(GetQuest(def.id));
            }
        }

        return result.ToArray();
    }

    // ================= СОХРАНЕНИЕ / ВОССТАНОВЛЕНИЕ =================

    public void GetSaveState(out int cycle, out string activeId, out int activeProgress, out List<string> completedIds)
    {
        cycle = _questCycle;
        activeId = _activeQuestId;

        activeProgress = 0;
        if (!string.IsNullOrWhiteSpace(_activeQuestId) && _quests.TryGetValue(Norm(_activeQuestId), out var q))
            activeProgress = q.current;

        completedIds = new List<string>(_completedIds);
    }

    public void RestoreFromSave(int cycle, string activeId, int activeProgress, List<string> completedIds)
    {
        _questCycle = Mathf.Max(0, cycle);

        _completedIds.Clear();
        if (completedIds != null)
        {
            for (int i = 0; i < completedIds.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(completedIds[i]))
                    _completedIds.Add(completedIds[i]);
            }
        }

        BuildFromInspector();
        ApplyCycleToAllDefinitions();
        ApplyCompletedVisualState();

        if (!string.IsNullOrWhiteSpace(activeId))
        {
            SetActive(activeId, resetProgress: false);

            var active = GetActiveQuest();
            if (active != null)
            {
                int t = GetTarget(active.def);
                active.current = Mathf.Clamp(activeProgress, 0, t);
                active.completed = active.current >= t;
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(startQuestId))
                SetActive(startQuestId, resetProgress: true);
            else
                _activeQuestId = null;
        }

        QuestsChanged?.Invoke();
    }

    // ================= ВНУТРЕННЯЯ ЛОГИКА =================

    private void BuildFromInspector()
    {
        _quests.Clear();

        if (initialQuests == null)
            return;

        foreach (var def in initialQuests)
        {
            if (def == null || string.IsNullOrWhiteSpace(def.id))
                continue;

            if (def.baseTarget <= 0)
                def.baseTarget = Mathf.Max(0, def.target);

            _quests[Norm(def.id)] = new QuestProgress
            {
                def = def,
                current = 0,
                completed = false
            };
        }

        if (!string.IsNullOrWhiteSpace(_activeQuestId) && !_quests.ContainsKey(Norm(_activeQuestId)))
            _activeQuestId = null;
    }

    private void SetActive(string id, bool resetProgress)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            _activeQuestId = null;
            return;
        }

        var key = Norm(id);
        if (!_quests.TryGetValue(key, out var q) || q == null || q.def == null)
        {
            _activeQuestId = null;
            return;
        }

        _activeQuestId = q.def.id;

        if (resetProgress)
            q.current = 0;
    }

    private void AdvanceActiveByTag(string tag, int delta)
    {
        var active = GetActiveQuest();
        if (active == null || active.def == null)
            return;

        if (!string.Equals(Norm(active.def.targetTag), Norm(tag), StringComparison.OrdinalIgnoreCase))
            return;

        int target = GetTarget(active.def);
        if (target <= 0)
        {
            CompleteQuest(active.def);
            return;
        }

        active.current = Mathf.Clamp(active.current + delta, 0, target);
        active.completed = active.current >= target;

        QuestsChanged?.Invoke();
        SaveRequested?.Invoke();

        if (active.completed)
            CompleteQuest(active.def);
    }

    private void CompleteQuest(QuestDefinition def)
    {
        if (def == null || string.IsNullOrWhiteSpace(def.id))
            return;

        var q = GetQuest(def.id);
        if (q != null)
        {
            q.completed = true;
            q.current = GetTarget(def);
        }

        if (!_completedIds.Contains(def.id))
            _completedIds.Add(def.id);

        ShowToast($"Поздравляем! Задание выполнено: {def.title}");

        if (!string.IsNullOrWhiteSpace(def.nextQuestId))
        {
            SetActive(def.nextQuestId, resetProgress: true);
            QuestsChanged?.Invoke();
            SaveRequested?.Invoke();
            return;
        }

        StartNextCycle();
    }

    private void StartNextCycle()
    {
        _questCycle++;

        _completedIds.Clear();

        foreach (var kv in _quests)
        {
            if (kv.Value == null) continue;
            kv.Value.current = 0;
            kv.Value.completed = false;
        }

        ApplyCycleToAllDefinitions();
        ApplyCompletedVisualState();

        if (!string.IsNullOrWhiteSpace(startQuestId))
            SetActive(startQuestId, resetProgress: true);
        else
            _activeQuestId = null;

        ShowToast($"Новый круг заданий! Уровень: {_questCycle}");

        QuestsChanged?.Invoke();
        SaveRequested?.Invoke();
    }

    private void ApplyCompletedVisualState()
    {
        for (int i = 0; i < _completedIds.Count; i++)
        {
            var id = _completedIds[i];
            var q = GetQuest(id);
            if (q == null || q.def == null) continue;

            q.completed = true;
            q.current = GetTarget(q.def);
        }
    }

    private void ApplyCycleToAllDefinitions()
    {
        foreach (var kv in _quests)
        {
            var q = kv.Value;
            if (q?.def == null) continue;

            q.def.target = GetTarget(q.def);
        }
    }

    private int GetTarget(QuestDefinition def)
    {
        if (def == null) return 0;

        int baseT = Mathf.Max(0, def.baseTarget);
        int add = def.addPerCycle;
        int t = baseT + _questCycle * add;

        return Mathf.Max(0, t);
    }

    private void ShowToast(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (toastUI == null)
            toastUI = FindToastComponentInScene();

        if (toastUI == null)
        {
            Debug.Log($"[QuestToast] {message}");
            return;
        }

        toastUI.gameObject.SendMessage("ShowMessage", message, SendMessageOptions.DontRequireReceiver);
    }

    private Component FindToastComponentInScene()
    {
        var all = FindObjectsOfType<MonoBehaviour>(true);
        for (int i = 0; i < all.Length; i++)
        {
            var mb = all[i];
            if (mb == null) continue;

            if (mb.GetType().Name == "QuestToastUI")
                return mb;
        }

        return null;
    }

    // ================= НАГРАДА ВАЛЮТОЙ =================

    private static bool TryAddMoneyToCurrentUser(int amount)
    {
        if (amount <= 0)
            return false;

        Type t = null;

        t = Type.GetType("UserManager");

        if (t == null)
        {
            var asms = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < asms.Length; i++)
            {
                t = asms[i].GetType("UserManager");
                if (t != null)
                    break;
            }
        }

        if (t == null)
        {
            var asms = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < asms.Length; i++)
            {
                Type[] types;
                try { types = asms[i].GetTypes(); }
                catch { continue; }

                for (int j = 0; j < types.Length; j++)
                {
                    if (types[j] != null && types[j].Name == "UserManager")
                    {
                        t = types[j];
                        break;
                    }
                }
                if (t != null)
                    break;
            }
        }

        if (t == null)
            return false;

        var pInst = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        if (pInst == null)
            return false;

        var inst = pInst.GetValue(null);
        if (inst == null)
            return false;

        var mAdd = t.GetMethod("AddMoneyToCurrentUser", BindingFlags.Public | BindingFlags.Instance);
        if (mAdd == null)
            return false;

        mAdd.Invoke(inst, new object[] { amount });
        return true;
    }

    private static string Norm(string s)
        => (s ?? string.Empty).Trim().ToLowerInvariant();

    public void ResetForNewGame()
    {
        _questCycle = 0;
        _completedIds.Clear();
        _activeQuestId = null;

        _bonusQuest = null;

        foreach (var kv in _quests)
        {
            if (kv.Value == null) continue;
            kv.Value.current = 0;
            kv.Value.completed = false;
        }

        ApplyCycleToAllDefinitions();
        ApplyCompletedVisualState();

        if (!string.IsNullOrWhiteSpace(startQuestId))
            SetActive(startQuestId, resetProgress: true);

        QuestsChanged?.Invoke();
    }
    
    private void Update()
    {
        if (!_bonusQuestTimerActive)
            return;

        if (_bonusQuest == null || _bonusQuest.completed)
            return;

        _bonusQuestTimer -= Time.deltaTime;

        if (_bonusQuestTimer <= 0f)
        {
            ExpireBonusQuest();
        }
    }
    private void ExpireBonusQuest()
    {
        _bonusQuestTimerActive = false;

        if (_bonusQuest != null)
        {
            ShowToast("Время бонусного задания истекло.");
        }

        _bonusQuest = null;

        QuestsChanged?.Invoke();
        SaveRequested?.Invoke();
    }


}
