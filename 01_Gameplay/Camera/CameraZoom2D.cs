using UnityEngine;
using UnityEngine.EventSystems;

public class CameraZoom2D : MonoBehaviour
{
    // Скорость изменения зума
    [SerializeField] private float zoomSpeed = 5f;

    // Минимальный и максимальный размер камеры
    [SerializeField] private float minZoom = 4f;
    [SerializeField] private float maxZoom = 20f;

    // Ссылка на камеру
    private Camera cam;

    private void Awake()
    {
        // Получаем компонент камеры
        cam = GetComponent<Camera>();

        // Если скрипт висит не на камере, ищем основную
        if (cam == null)
            cam = Camera.main;
    }

    private void Update()
    {
        // Если курсор находится над UI, зум не выполняется
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // Получаем значение прокрутки колеса мыши
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        // Если колесо не крутилось, выходим
        if (Mathf.Abs(scroll) < 0.0001f)
            return;

        // Изменяем размер ортографической камеры
        cam.orthographicSize -= scroll * zoomSpeed;

        // Ограничиваем зум допустимыми значениями
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
    }
}