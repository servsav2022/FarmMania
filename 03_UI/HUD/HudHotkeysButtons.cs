using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HudHotkeysButtons : MonoBehaviour
{
    [Header("Кнопки верхней панели HUD")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;

    private void Update()
    {
        // Если пользователь сейчас работает с UI (например, поле ввода), не перехватываем клавиши
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            return;

        // F5 - сохранить
        if (Input.GetKeyDown(KeyCode.F5) && saveButton != null)
            saveButton.onClick.Invoke();

        // F2 - загрузить
        if (Input.GetKeyDown(KeyCode.F2) && loadButton != null)
            loadButton.onClick.Invoke();
    }
}