using UnityEngine;
using TMPro; // Для работы с TextMeshPro InputField
using UnityEngine.UI; // Для работы с кнопками (Button)

public class AuthController : MonoBehaviour
{
    [Header("Main Menu")]
    public MainMenuController mainMenuController;
    // 1. ССЫЛКИ НА ПАНЕЛИ 
    [Header("Основные панели")]
    public GameObject panelChoice;    // Панель выбора (Войти/Зарегистрироваться)
    public GameObject panelLogin;     // Панель входа
    public GameObject panelRegister;  // Панель регистрации
    public GameObject panelAuth;      // Главная панель Panel_Auth (родительская)

    // 2. ССЫЛКИ НА ПОЛЯ ВВОДА (TextMeshPro) для ЛОГИНА
    [Header("Поля для входа (Login)")]
    public TMP_InputField loginUsernameInput;
    public TMP_InputField loginPasswordInput;

    // 3. ССЫЛКИ НА ПОЛЯ ВВОДА (TextMeshPro) для РЕГИСТРАЦИИ
    [Header("Поля для регистрации (Register)")]
    public TMP_InputField regUsernameInput;
    public TMP_InputField regPasswordInput;
    public TMP_InputField regConfirmPasswordInput;

    // 4. МЕТОД ДЛЯ ПЕРЕКЛЮЧЕНИЯ МЕЖДУ ПАНЕЛЯМИ
    // Показывает одну панель и скрывает две другие
    public void ShowPanel(GameObject panelToShow)
    {
        // Скрываем все
        panelChoice.SetActive(false);
        panelLogin.SetActive(false);
        panelRegister.SetActive(false);

        // Показываем нужную
        panelToShow.SetActive(true);
    }

    // 5. УДОБНЫЕ МЕТОДЫ ДЛЯ НАЗНАЧЕНИЯ НА КНОПКИ
    public void ShowLoginPanel() => ShowPanel(panelLogin);
    public void ShowRegisterPanel() => ShowPanel(panelRegister);
    public void ShowChoicePanel() => ShowPanel(panelChoice);

    // 6. ОБРАБОТКА КНОПКИ "ВОЙТИ" (на панели Login)
    public void OnLoginButtonClick()
    {
        // Получаем текст из полей ввода
        string username = loginUsernameInput.text;
        string password = loginPasswordInput.text;

        // Простейшая проверка на пустоту
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.Log("Ошибка: Заполните все поля для входа.");
            return;
        }

        // Пытаемся войти через UserManager
        bool loginSuccess = UserManager.Instance.LoginUser(username, password);

        if (loginSuccess)
        {
            Debug.Log("Ура! Вход выполнен успешно для пользователя: " + username);

            panelAuth.SetActive(false);
            
            if (mainMenuController != null)
            {
                panelAuth.SetActive(false);
                mainMenuController.ShowMainMenu();
            }
            else
            {
                Debug.LogError("MainMenuController не назначен в AuthController!");
            }
        }
        else
        {
            Debug.Log("Ошибка входа. Проверьте логин и пароль.");
            // Можно, например, очистить поле пароля
            loginPasswordInput.text = "";
        }
    }

    // 7. ОБРАБОТКА КНОПКИ "СОЗДАТЬ АККАУНТ" (на панели Register)
    public void OnRegisterButtonClick()
    {
        // === ДИАГНОСТИКА ===
        if (UserManager.Instance == null)
        {
            Debug.LogError("UserManager.Instance равен NULL! Проверь:");
            Debug.LogError("1. Добавлен ли UserManager на сцену 0_Bootstrap?");
            Debug.LogError("2. Правильный ли порядок сцен в Build Settings?");
            Debug.LogError("3. Запускаешь игру из сцены 0_Bootstrap?");
            return; // Прекращаем выполнение
        }
        if (regUsernameInput == null) Debug.LogError("regUsernameInput не назначен!");
        if (regPasswordInput == null) Debug.LogError("regPasswordInput не назначен!");
        if (regConfirmPasswordInput == null) Debug.LogError("regConfirmPasswordInput не назначен!");
        // ===================
        // Получаем текст из полей ввода
        string username = regUsernameInput.text;
        string password = regPasswordInput.text;
        string confirmPassword = regConfirmPasswordInput.text;

        // Проверяем, что все поля заполнены
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            Debug.Log("Ошибка: Заполните все поля для регистрации.");
            return;
        }

        // Проверяем, что пароли совпадают
        if (password != confirmPassword)
        {
            Debug.Log("Ошибка: Пароли не совпадают.");
            regPasswordInput.text = ""; // Очищаем оба поля с паролями
            regConfirmPasswordInput.text = "";
            return;
        }

        // Пытаемся зарегистрировать через UserManager
        bool registerSuccess = UserManager.Instance.RegisterUser(username, password);

        if (registerSuccess)
        {
            Debug.Log("Отлично! Аккаунт '" + username + "' создан.");
            // Очищаем поля и переходим на панель входа
            regUsernameInput.text = "";
            regPasswordInput.text = "";
            regConfirmPasswordInput.text = "";
            ShowPanel(panelLogin); // Предлагаем войти с новыми данными
        }
        else
        {
            Debug.Log("Ошибка регистрации. Возможно, такое имя уже занято.");
        }
    }
    public void ExitGame()
    {
        GameManager.Instance.QuitGame();
    }

    // 8. Метод Start для начальной настройки
    void Start()
    {
        // Убедимся, что при старте игры видна только панель выбора
        ShowPanel(panelChoice);
        Debug.Log("AuthController готов к работе.");
    }
}