using UnityEngine;

public class HelpWindowController : MonoBehaviour
{
    [SerializeField] private GameObject helpPanel;

    public void ToggleHelp()
    {
        if (helpPanel == null)
            return;

        helpPanel.SetActive(!helpPanel.activeSelf);
    }

    public void CloseHelp()
    {
        if (helpPanel == null)
            return;

        helpPanel.SetActive(false);
    }
}