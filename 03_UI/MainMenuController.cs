using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public static MainMenuController Instance { get; private set; }
    [Header("Panels")]
    [SerializeField] private GameObject panelAuth;
    [SerializeField] private GameObject panelMainMenu;
    [SerializeField] private GameObject panelLoadMenu;
    [SerializeField] private GameObject panelSettings;
    
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        HideAll();

        var gm = GameManager.Instance;

        if (gm == null)
        {
            ShowAuth();
            return;
        }

        // Вернулись из игры
        if (gm.ReturnFromGame)
        {
            ShowMainMenu();
            gm.ReturnFromGame = false;
            return;
        }

        // Уже залогинен
        if (gm.IsLoggedIn)
        {
            ShowMainMenu();
            return;
        }

        // Первый запуск
        ShowAuth();
    }



    private void HideAll()
    {
        panelAuth.SetActive(false);
        panelMainMenu.SetActive(false);
        panelLoadMenu.SetActive(false);
        panelSettings.SetActive(false);
    }

    private void ShowAuth()
    {
        HideAll();
        panelAuth.SetActive(true);
    }

    public void ShowMainMenu()
    {
        HideAll();
        panelMainMenu.SetActive(true);
        Debug.Log("[MainMenu] Главное меню открыто");
    }


    public void NewGame()
    {
        Debug.Log("[MainMenu] Новая игра");

        UserManager.Instance.CreateNewSaveSlot();

        var qm = QuestManagerMono.Instance;
        if (qm != null)
            qm.ResetForNewGame();

        SceneManager.LoadScene("2_Farm");
    }


    public void ContinueGame()
    {
        Debug.Log("[MainMenu] Continue game");
        UserManager.Instance.LoadBestAvailableSave();
        SceneManager.LoadScene("2_Farm");
    }


    public void OpenLoadMenu()
    {
        panelMainMenu.SetActive(false);
        panelLoadMenu.SetActive(true);
    }

    public void OpenSettings()
    {
        panelMainMenu.SetActive(false);
        panelSettings.SetActive(true);
    }

    public void ExitGame()
    {
        GameManager.Instance.QuitGame();
    }
}