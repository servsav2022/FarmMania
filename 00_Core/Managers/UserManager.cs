using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UserManager : MonoBehaviour
{
    public static UserManager Instance { get; private set; }
    public int SelectedManualSlot { get; private set; } = 1;

    public bool IsLoadingSave { get; private set; }

    // Текущее состояние
    public UserAccountData CurrentUser { get; private set; }
    public GameSaveData CurrentSave { get; private set; }
    public string SelectedSeedItemId { get; private set; }

    // Файлы
    private string usersFilePath;

    // Аккаунты
    private readonly Dictionary<string, UserAccountData> accounts = new();
    private readonly Dictionary<string, string> usernameToIdMap = new();

    // Мир: если мы загрузили сейв в меню, то грядки нужно применить позже (когда сцена фермы загрузится)
    private bool pendingApplyWorld;

    // Задания: то же самое, если QuestSystem еще не создан, восстановим позже
    private bool pendingApplyQuests;

    // Позиция игрока: если Player еще не появился в сцене, восстановим позже
    private bool pendingApplyPlayerPosition;

    // Резервные сохранения
    [Header("Backup")]
    [SerializeField] private float backupIntervalSeconds = 120f;
    private float backupTimer;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        usersFilePath = Path.Combine(Application.persistentDataPath, "users.json");
        LoadAllAccounts();

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        Debug.Log($"[UserManager] Loaded {accounts.Count} accounts");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (backupIntervalSeconds <= 0f)
            return;

        if (IsLoadingSave || CurrentUser == null || CurrentSave == null)
            return;

        backupTimer += Time.unscaledDeltaTime;
        if (backupTimer >= backupIntervalSeconds)
        {
            backupTimer = 0f;
            SaveBackup();
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
            SaveBackup();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            SaveBackup();
    }

    private void OnApplicationQuit()
    {
        SaveBackup();
        SaveAuto();
        Debug.Log("[AutoSave] Saved on exit");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryApplyWorldFromSave();
        TryApplyQuestsFromSave();
        TryApplyPlayerPositionFromSave();
    }

    private void LoadAllAccounts()
    {
        if (!File.Exists(usersFilePath))
        {
            SaveAllAccounts();
            return;
        }

        var json = File.ReadAllText(usersFilePath);
        var list = JsonConvert.DeserializeObject<List<UserAccountData>>(json) ?? new();

        accounts.Clear();
        usernameToIdMap.Clear();

        foreach (var acc in list)
        {
            if (string.IsNullOrEmpty(acc.UserId))
                acc.UserId = Guid.NewGuid().ToString();

            acc.SaveFiles ??= new List<string>();

            accounts[acc.UserId] = acc;
            usernameToIdMap[acc.Username.ToLower()] = acc.UserId;
        }
    }

    private void SaveAllAccounts()
    {
        var json = JsonConvert.SerializeObject(accounts.Values, Formatting.Indented);
        File.WriteAllText(usersFilePath, json);
    }

    // Авторизация

    public bool RegisterUser(string username, string password)
    {
        string key = username.ToLower();
        if (usernameToIdMap.ContainsKey(key))
            return false;

        string salt = GenerateSalt();
        string hash = HashPassword(password, salt);

        var user = new UserAccountData
        {
            UserId = Guid.NewGuid().ToString(),
            Username = username,
            PasswordHash = hash,
            Salt = salt,
            CreatedDate = DateTime.Now,
            SaveFiles = new List<string>(),
            LastSaveFile = null
        };

        accounts[user.UserId] = user;
        usernameToIdMap[key] = user.UserId;

        SaveAllAccounts();
        return true;
    }

    public bool LoginUser(string username, string password)
    {
        if (!usernameToIdMap.TryGetValue(username.ToLower(), out var id))
        {
            Debug.Log($"[Auth] Login failed: user '{username}' not found");
            return false;
        }

        var acc = accounts[id];
        if (HashPassword(password, acc.Salt) != acc.PasswordHash)
        {
            Debug.Log($"[Auth] Login failed: wrong password for '{username}'");
            return false;
        }

        CurrentUser = acc;
        Debug.Log($"[Auth] Login success: {username}");

        LoadBestAvailableSave();
        return true;
    }

    // Слоты сохранений

    public void CreateNewSaveSlot()
    {
        if (CurrentUser == null)
            return;

        string saveId = $"{CurrentUser.Username}_{DateTime.Now:yyyyMMdd_HHmmss}";
        string firstQuestId = "q_plant_1";

        CurrentSave = new GameSaveData
        {
            saveId = saveId,
            savedAt = DateTime.Now,
            money = 30,
            inventory = new List<InventoryItem>(),
            plots = new List<PlotSaveData>(),

            // Задания
            questCycle = 0,
            activeQuestId = firstQuestId,
            activeQuestProgress = 0,
            completedQuestIds = new List<string>(),

            // Позиция игрока
            hasPlayerPosition = false,
            playerPosX = 0f,
            playerPosY = 0f,
            playerPosZ = 0f
        };

        CurrentUser.LastSaveFile = saveId;
        CurrentUser.SaveFiles.Add(saveId);

        SaveManager.SaveGame(saveId, CurrentSave);
        SaveManager.SaveGameBackup(saveId, CurrentSave);

        SaveAllAccounts();

        Debug.Log($"[UserManager] Created save slot: {saveId}");
    }

    // Сохранение и загрузка

    public void SaveCurrentUserGame()
    {
        if (IsLoadingSave || CurrentUser == null || CurrentSave == null)
            return;

        var farm = FindObjectOfType<FarmGridController>();
        if (farm != null)
            CurrentSave.plots = farm.BuildPlotSaveData();

        CaptureQuestStateIntoSave();
        CapturePlayerPositionIntoSave();

        CurrentSave.savedAt = DateTime.Now;

        SaveManager.SaveGame(CurrentSave.saveId, CurrentSave);
        SaveManager.SaveGameBackup(CurrentSave.saveId, CurrentSave);

        Debug.Log("[Save] Manual save completed");
    }

    public void LoadCurrentUserGame()
    {
        if (CurrentUser == null || string.IsNullOrEmpty(CurrentUser.LastSaveFile))
            return;

        LoadSaveInternal(CurrentUser.LastSaveFile);
    }

    private void LoadSaveInternal(string saveId)
    {
        var data = SaveManager.LoadGameOrBackup(saveId);
        if (data == null)
        {
            Debug.LogWarning("[UserManager] Save missing or broken, creating new");
            CreateNewSaveSlot();
            return;
        }

        IsLoadingSave = true;

        CurrentSave = data;

        CurrentSave.inventory ??= new List<InventoryItem>();
        CurrentSave.plots ??= new List<PlotSaveData>();
        CurrentSave.completedQuestIds ??= new List<string>();

        pendingApplyWorld = true;
        TryApplyWorldFromSave();

        pendingApplyQuests = true;
        TryApplyQuestsFromSave();

        pendingApplyPlayerPosition = true;
        TryApplyPlayerPositionFromSave();

        IsLoadingSave = false;

        Debug.Log($"[UserManager] Save loaded: {saveId} (plots: {CurrentSave.plots?.Count ?? 0})");
    }

    // Пытаемся применить сохраненный мир, когда FarmGridController реально есть в сцене
    private void TryApplyWorldFromSave()
    {
        if (!pendingApplyWorld)
            return;

        if (CurrentSave == null)
            return;

        var farm = FindObjectOfType<FarmGridController>();
        if (farm == null)
            return;

        farm.ApplyPlotSaveData(CurrentSave.plots);
        pendingApplyWorld = false;

        Debug.Log($"[UserManager] World applied from save (plots: {CurrentSave.plots?.Count ?? 0})");
    }

    // Пытаемся применить состояние заданий, когда QuestManagerMono реально есть в сцене
    private void TryApplyQuestsFromSave()
    {
        if (!pendingApplyQuests)
            return;

        if (CurrentSave == null)
            return;

        var qm = QuestManagerMono.Instance != null
            ? QuestManagerMono.Instance
            : FindObjectOfType<QuestManagerMono>(true);

        if (qm == null)
            return;

        CurrentSave.completedQuestIds ??= new List<string>();

        qm.RestoreFromSave(
            CurrentSave.questCycle,
            CurrentSave.activeQuestId,
            CurrentSave.activeQuestProgress,
            CurrentSave.completedQuestIds
        );

        qm.SaveRequested -= OnQuestSaveRequested;
        qm.SaveRequested += OnQuestSaveRequested;

        pendingApplyQuests = false;

        Debug.Log("[UserManager] Quests restored from save");
    }

    // Пытаемся применить позицию игрока, когда PlayerHybridMove2D реально есть в сцене
    private void TryApplyPlayerPositionFromSave()
    {
        if (!pendingApplyPlayerPosition)
            return;

        if (CurrentSave == null)
            return;

        if (!CurrentSave.hasPlayerPosition)
        {
            pendingApplyPlayerPosition = false;
            return;
        }

        var player = FindObjectOfType<PlayerHybridMove2D>(true);
        if (player == null)
            return;

        player.transform.position = new Vector3(
            CurrentSave.playerPosX,
            CurrentSave.playerPosY,
            CurrentSave.playerPosZ
        );

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        pendingApplyPlayerPosition = false;

        Debug.Log("[UserManager] Player position restored from save");
    }

    // Сохраняем текущую позицию игрока в CurrentSave
    private void CapturePlayerPositionIntoSave()
    {
        if (CurrentSave == null)
            return;

        var player = FindObjectOfType<PlayerHybridMove2D>(true);
        if (player == null)
            return;

        Vector3 p = player.transform.position;

        CurrentSave.hasPlayerPosition = true;
        CurrentSave.playerPosX = p.x;
        CurrentSave.playerPosY = p.y;
        CurrentSave.playerPosZ = p.z;
    }

    // Деньги

    public int GetCurrentUserMoney()
        => CurrentSave?.money ?? 0;

    public void AddMoneyToCurrentUser(int delta)
    {
        if (CurrentSave == null || IsLoadingSave)
            return;

        CurrentSave.money = Mathf.Max(0, CurrentSave.money + delta);

        if (delta > 0 && QuestManagerMono.Instance != null)
            QuestManagerMono.Instance.ReportMoneyEarned(delta);
    }

    // Инвентарь

    public List<InventoryItem> GetCurrentInventory()
        => CurrentSave?.inventory;

    public int GetItemAmount(string itemId)
    {
        if (CurrentSave == null)
            return 0;

        var e = CurrentSave.inventory.Find(i => i.itemId == itemId);
        return e?.amount ?? 0;
    }

    public void AddItemToInventory(string itemId, int amount)
    {
        if (CurrentSave == null || IsLoadingSave || amount <= 0)
            return;

        var e = CurrentSave.inventory.Find(i => i.itemId == itemId);
        if (e == null)
            CurrentSave.inventory.Add(new InventoryItem { itemId = itemId, amount = amount });
        else
            e.amount += amount;
    }

    public bool TryRemoveItemFromInventory(string itemId, int amount)
    {
        if (CurrentSave == null || IsLoadingSave || amount <= 0)
            return false;

        var e = CurrentSave.inventory.Find(i => i.itemId == itemId);
        if (e == null || e.amount < amount)
            return false;

        e.amount -= amount;
        if (e.amount <= 0)
            CurrentSave.inventory.Remove(e);

        return true;
    }

    // Совместимость (старые имена)

    public void AddItemToCurrentUser(string id, int amount)
        => AddItemToInventory(id, amount);

    public bool TryUseItemFromCurrentUser(string id, int amount)
        => TryRemoveItemFromInventory(id, amount);

    public int GetItemCountForCurrentUser(string id)
        => GetItemAmount(id);

    // Автосохранение

    private string GetAutoSaveId()
        => $"autosave_{CurrentUser.Username}";

    public void SaveAuto()
    {
        if (CurrentUser == null || CurrentSave == null)
            return;

        var farm = FindObjectOfType<FarmGridController>();
        if (farm != null)
            CurrentSave.plots = farm.BuildPlotSaveData();

        CaptureQuestStateIntoSave();
        CapturePlayerPositionIntoSave();

        SaveManager.SaveGame(GetAutoSaveId(), CurrentSave);
        Debug.Log("[AutoSave] Saved");
    }

    // Резервное сохранение, максимально близкое к текущему состоянию
    public void SaveBackup()
    {
        if (IsLoadingSave || CurrentUser == null || CurrentSave == null)
            return;

        var farm = FindObjectOfType<FarmGridController>();
        if (farm != null)
            CurrentSave.plots = farm.BuildPlotSaveData();

        CaptureQuestStateIntoSave();
        CapturePlayerPositionIntoSave();

        string id = !string.IsNullOrEmpty(CurrentSave.saveId)
            ? CurrentSave.saveId
            : GetAutoSaveId();

        SaveManager.SaveGameBackup(id, CurrentSave);
    }

    // Безопасность

    private string GenerateSalt()
    {
        byte[] salt = new byte[32];
        RandomNumberGenerator.Fill(salt);
        return Convert.ToBase64String(salt);
    }

    private string HashPassword(string password, string salt)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(
            sha.ComputeHash(Encoding.UTF8.GetBytes(password + salt))
        );
    }

    // Выбор семян

    public void SelectSeed(string seedItemId)
    {
        SelectedSeedItemId = seedItemId;
        Debug.Log($"[SeedSelect] Selected item: {seedItemId}");
    }

    public void ClearSelectedSeed()
    {
        SelectedSeedItemId = null;
    }

    // Логика выбора сохранения

    public void LoadBestAvailableSave()
    {
        if (CurrentUser == null)
        {
            Debug.LogWarning("[UserManager] LoadBestAvailableSave: CurrentUser == null");
            return;
        }

        string autoId = GetAutoSaveId();
        Debug.Log($"[UserManager] Trying autosave: {autoId}");

        var autoSave = SaveManager.LoadGameOrBackup(autoId);
        if (autoSave != null)
        {
            Debug.Log("[UserManager] Autosave found -> loading");
            LoadSaveInternal(autoId);
            return;
        }

        if (!string.IsNullOrEmpty(CurrentUser.LastSaveFile))
        {
            Debug.Log($"[UserManager] Autosave not found -> loading last manual save: {CurrentUser.LastSaveFile}");
            LoadSaveInternal(CurrentUser.LastSaveFile);
            return;
        }

        Debug.Log("[UserManager] No saves found -> creating new save");
        CreateNewSaveSlot();
    }

    // Ручные слоты (1-3)

    public string GetManualSlotId(int slotIndex)
    {
        if (CurrentUser == null) return null;
        return $"{CurrentUser.Username}_slot_{slotIndex}";
    }

    public void SaveToManualSlot(int slotIndex)
    {
        if (CurrentUser == null || CurrentSave == null)
        {
            Debug.LogWarning("[Save] SaveToManualSlot: no user or save");
            return;
        }

        string saveId = GetManualSlotId(slotIndex);
        if (string.IsNullOrEmpty(saveId))
            return;

        var farm = FindObjectOfType<FarmGridController>();
        if (farm != null)
            CurrentSave.plots = farm.BuildPlotSaveData();

        CaptureQuestStateIntoSave();
        CapturePlayerPositionIntoSave();

        CurrentSave.saveId = saveId;
        CurrentSave.savedAt = DateTime.Now;

        SaveManager.SaveGame(saveId, CurrentSave);
        SaveManager.SaveGameBackup(saveId, CurrentSave);

        if (!CurrentUser.SaveFiles.Contains(saveId))
            CurrentUser.SaveFiles.Add(saveId);

        CurrentUser.LastSaveFile = saveId;
        SaveAllAccounts();

        Debug.Log($"[Save] Manual slot {slotIndex} saved");
    }

    public bool LoadManualSlot(int slotIndex)
    {
        if (CurrentUser == null)
            return false;

        string saveId = GetManualSlotId(slotIndex);
        if (string.IsNullOrEmpty(saveId))
            return false;

        var data = SaveManager.LoadGameOrBackup(saveId);
        if (data == null)
        {
            Debug.Log($"[Load] Manual slot {slotIndex} is empty");
            return false;
        }

        LoadSaveInternal(saveId);
        Debug.Log($"[Load] Manual slot {slotIndex} loaded");
        return true;
    }

    public void DeleteManualSlot(int slotIndex)
    {
        if (CurrentUser == null)
            return;

        string saveId = GetManualSlotId(slotIndex);
        if (string.IsNullOrEmpty(saveId))
            return;

        SaveManager.DeleteSave(saveId);
        CurrentUser.SaveFiles.Remove(saveId);

        if (CurrentUser.LastSaveFile == saveId)
            CurrentUser.LastSaveFile = null;

        SaveAllAccounts();

        Debug.Log($"[Save] Manual slot {slotIndex} deleted");
    }

    public void SetSelectedManualSlot(int slotIndex)
    {
        SelectedManualSlot = Mathf.Clamp(slotIndex, 1, 3);
        Debug.Log($"[Save] Selected manual slot: {SelectedManualSlot}");
    }

    public bool LoadLatestManualSave()
    {
        GameSaveData latest = null;
        string latestId = null;

        for (int slot = 1; slot <= 3; slot++)
        {
            string saveId = GetManualSlotId(slot);
            var data = SaveManager.LoadGameOrBackup(saveId);

            if (data == null)
                continue;

            if (latest == null || data.savedAt > latest.savedAt)
            {
                latest = data;
                latestId = saveId;
            }
        }

        if (latestId == null)
        {
            Debug.Log("[Load] No manual saves available");
            return false;
        }

        LoadSaveInternal(latestId);
        Debug.Log($"[Load] Latest manual save loaded: {latestId}");
        return true;
    }

    public bool HasAnyManualSave()
    {
        if (CurrentUser == null)
            return false;

        for (int slot = 1; slot <= 3; slot++)
        {
            string saveId = GetManualSlotId(slot);
            if (SaveManager.LoadGameOrBackup(saveId) != null)
                return true;
        }

        return false;
    }

    // ================= ЗАДАНИЯ: СОХРАНЕНИЕ И ВОССТАНОВЛЕНИЕ =================

    private void CaptureQuestStateIntoSave()
    {
        if (CurrentSave == null)
            return;

        var qm = QuestManagerMono.Instance;
        if (qm == null)
            return;

        qm.GetSaveState(out var cycle, out var activeId, out var activeProgress, out var completed);

        CurrentSave.questCycle = cycle;
        CurrentSave.activeQuestId = activeId;
        CurrentSave.activeQuestProgress = activeProgress;

        CurrentSave.completedQuestIds = completed ?? new List<string>();
    }

    private void OnQuestSaveRequested()
    {
        if (IsLoadingSave)
            return;

        CaptureQuestStateIntoSave();
        SaveAuto();
    }
}
