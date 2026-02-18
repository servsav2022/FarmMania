using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button button;

    public void Bind(string title, System.Action onClick)
    {
        if (titleText != null)
            titleText.text = title;

        if (button == null)
            return;

        button.onClick.RemoveAllListeners();

        if (onClick != null)
        {
            button.interactable = true;
            button.onClick.AddListener(() => onClick());
        }
        else
        {
            button.interactable = false;
        }
    }

#if UNITY_EDITOR
    private void Reset()
    {
        button = GetComponent<Button>();
        titleText = GetComponentInChildren<TMP_Text>();
    }
#endif
}