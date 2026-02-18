using System.Collections;
using TMPro;
using UnityEngine;

public class QuestToastUI : MonoBehaviour
{
    // Объект, который визуально показываем/прячем (можно оставить пустым, тогда будет this.gameObject)
    [SerializeField] private GameObject root;

    // Текст сообщения
    [SerializeField] private TMP_Text messageText;

    // Сколько секунд показывать сообщение
    [SerializeField] private float showSeconds = 3.5f;

    private CanvasGroup canvasGroup;
    private Coroutine routine;

    private void Awake()
    {
        if (root == null)
            root = gameObject;

        // Гарантируем CanvasGroup, чтобы скрывать без отключения объекта
        canvasGroup = root.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = root.AddComponent<CanvasGroup>();

        // Сразу прячем
        HideInstant();
    }

    // Показ сообщения (можно вызывать хоть каждый кадр, старое сообщение будет заменено)
    public void ShowMessage(string message)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(ShowRoutine(message));
    }

    private IEnumerator ShowRoutine(string message)
    {
        if (messageText != null)
            messageText.text = message;

        ShowInstant();

        float t = Mathf.Max(0.5f, showSeconds);
        yield return new WaitForSeconds(t);

        HideInstant();
        routine = null;
    }

    private void ShowInstant()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void HideInstant()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}