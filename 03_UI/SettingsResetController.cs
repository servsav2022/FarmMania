using UnityEngine;
using UnityEngine.UI;

public class SettingsResetController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button resetButton;

    [Header("Controllers")]
    [SerializeField] private DisplaySettingsController display;
    [SerializeField] private AudioSettingsController audio;

    private void Awake()
    {
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetAll);
        else
            Debug.LogWarning("[SettingsReset] Reset button is not assigned");
    }

    private void OnDestroy()
    {
        if (resetButton != null)
            resetButton.onClick.RemoveListener(ResetAll);
    }

    private void ResetAll()
    {
        if (display != null) display.ResetToDefaults();
        if (audio != null) audio.ResetToDefaults();

        Debug.Log("[SettingsReset] All settings reset");
    }
}