using UnityEngine;
using UnityEngine.Rendering;

public class ExitToMainMenuButton : MonoBehaviour
{
    public void ExitToMainMenu()
    {
        Debug.Log("[GameManager] Exit to Main Menu");

        if (UserManager.Instance != null)
            UserManager.Instance.SaveAuto();

        GameManager.Instance.LoadMainMenu(fromGame: true);
    }

}
