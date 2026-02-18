using TMPro;
using UnityEngine;

public class VersionInfo : MonoBehaviour
{
    [SerializeField] private TMP_Text versionText;

    private void Awake()
    {
        if (versionText == null)
            versionText = GetComponent<TMP_Text>();

        if (versionText != null)
            versionText.text = $"FarmMania v{Application.version}";
    }
}