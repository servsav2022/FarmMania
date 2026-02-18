using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool IsLoggedIn { get; private set; }
    public bool ReturnFromGame { get;  set; }


    public enum GameState { Menu, Playing, Paused }
    public GameState CurrentState { get; private set; }

    /// <summary>
    /// Текущий баланс активного пользователя.
    /// Если пользователь не залогинен — 0.
    /// </summary>
    public int Money => UserManager.Instance != null ? UserManager.Instance.GetCurrentUserMoney() : 0;

    private void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeGame()
    {
        CurrentState = GameState.Menu;
        Debug.Log("[GameManager] Инициализирован.");

        // Если запустили не из меню — возвращаемся в меню (по твоему старому поведению)
        if (SceneManager.GetActiveScene().name != "1_MainMenu")
            LoadMainMenu();
    }

    // ===== СЦЕНЫ / СОСТОЯНИЯ =====

    public void LoadMainMenu(bool fromGame = false)
    {
        ReturnFromGame = fromGame;
        SceneManager.LoadScene("1_MainMenu");
        CurrentState = GameState.Menu;
        Time.timeScale = 1f;
    }

    public void StartGame()
    {
        // Лучше по имени, чтобы не сломалось при перестановке Build Settings
        SceneManager.LoadScene("2_Farm");
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        Debug.Log("[GameManager] Игра началась! Баланс: " + Money);
    }

    public void TogglePause()
    {
        bool toPause = (CurrentState != GameState.Paused);
        CurrentState = toPause ? GameState.Paused : GameState.Playing;
        Time.timeScale = toPause ? 0f : 1f;
        Debug.Log("[GameManager] " + (toPause ? "Пауза" : "Продолжить"));
    }

    public void QuitGame()
    {
        Debug.Log("[GameManager] Выход из игры...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ===== ДЕНЬГИ =====

    public void AddMoney(int amount)
    {
        if (UserManager.Instance == null || UserManager.Instance.CurrentUser == null)
        {
            Debug.LogWarning("[GameManager] AddMoney: нет залогиненного пользователя.");
            return;
        }

        UserManager.Instance.AddMoneyToCurrentUser(amount);
        Debug.Log("[GameManager] Баланс изменён на " + amount + ". Новый: " + Money);
    }

    public bool TrySpendMoney(int amount)
    {
        if (amount <= 0)
            return true;

        if (Money < amount)
        {
            Debug.Log("[GameManager] Недостаточно денег. Нужно: " + amount + ", есть: " + Money);
            return false;
        }

        AddMoney(-amount);
        return true;
    }

    // ===== ИНВЕНТАРЬ =====

    public void AddItem(string itemId, int amount)
    {
        if (UserManager.Instance == null || UserManager.Instance.CurrentUser == null)
        {
            Debug.LogWarning("[GameManager] AddItem: нет активного пользователя.");
            return;
        }

        UserManager.Instance.AddItemToCurrentUser(itemId, amount);
    }

    public bool TryUseItem(string itemId, int amount = 1)
    {
        if (UserManager.Instance == null || UserManager.Instance.CurrentUser == null)
        {
            Debug.LogWarning("[GameManager] TryUseItem: нет активного пользователя.");
            return false;
        }

        return UserManager.Instance.TryUseItemFromCurrentUser(itemId, amount);
    }

    public int GetItemCount(string itemId)
    {
        if (UserManager.Instance == null)
            return 0;

        return UserManager.Instance.GetItemCountForCurrentUser(itemId);
    }
    public void SetLoggedIn()
    {
        IsLoggedIn = true;
    }

   
}
