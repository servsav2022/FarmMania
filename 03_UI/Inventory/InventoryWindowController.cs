using TMPro;
using UnityEngine;

public class InventoryWindowController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("UI")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private Transform listContent;
    [SerializeField] private InventoryListRowUI rowPrefab;

    [Header("Backdrop")]
    [SerializeField] private GameObject backdropObject;

    [Header("UX")]
    [SerializeField] private float doubleClickTime = 0.35f;

    private string _selectedItemId;
    public string SelectedItemId => _selectedItemId;

    private string _lastClickItemId;
    private float _lastClickTime;

    private void Awake()
    {
        if (backdropObject != null)
            backdropObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (backdropObject != null)
            backdropObject.SetActive(true);

        Refresh();
    }

    private void OnDisable()
    {
        if (backdropObject != null)
            backdropObject.SetActive(false);

        // Важно: НЕ чистим выбор при закрытии окна,
        // иначе будет пропадать выбранное семя при любом переключении UI.
    }

    public void Toggle()
    {
        if (gameObject.activeSelf)
            Close();
        else
            Open();
    }

    public void Open()
    {
        if (backdropObject != null)
            backdropObject.SetActive(true);

        gameObject.SetActive(true);
        Refresh();
    }

    // Обычное закрытие (например по кнопке инвентаря)
    public void Close()
    {
        if (backdropObject != null)
            backdropObject.SetActive(false);

        gameObject.SetActive(false);
    }

    // Закрытие кликом вне окна (это назначить на Button у InventoryBackdrop)
    public void CloseFromBackdrop()
    {
        Close();
    }

    public void ForceRefresh()
    {
        Refresh();
    }

    // ================= REFRESH =================

    private void Refresh()
    {
        RefreshMoney();
        RefreshList();
    }

    private void RefreshMoney()
    {
        if (moneyText == null)
            return;

        int money = UserManager.Instance?.CurrentSave?.money ?? 0;
        moneyText.text = $"Money: {money}";
    }

    private void RefreshList()
    {
        if (listContent == null || rowPrefab == null)
            return;

        for (int i = listContent.childCount - 1; i >= 0; i--)
            Destroy(listContent.GetChild(i).gameObject);

        var save = UserManager.Instance?.CurrentSave;
        if (save == null || save.inventory == null)
            return;

        foreach (var it in save.inventory)
        {
            if (it.amount <= 0)
                continue;

            string displayName =
                itemDatabase != null
                    ? itemDatabase.GetDisplayName(it.itemId)
                    : it.itemId;

            bool selected = Normalize(it.itemId) == Normalize(_selectedItemId);

            var row = Instantiate(rowPrefab, listContent);
            row.Bind(
                it.itemId,
                displayName,
                it.amount,
                0,
                selected,
                OnRowClicked
            );
        }
    }

    private void OnRowClicked(string itemId)
    {
        bool isDoubleClickSameItem =
            Normalize(_lastClickItemId) == Normalize(itemId) &&
            (Time.unscaledTime - _lastClickTime) <= doubleClickTime;

        _lastClickTime = Time.unscaledTime;
        _lastClickItemId = itemId;

        // Двойной клик по тому же предмету - снять выделение и семена "из руки"
        if (isDoubleClickSameItem)
        {
            ClearSelection();
            RefreshList();
            return;
        }

        _selectedItemId = itemId;

        // Если это семя - выбираем для посадки
        if (itemDatabase != null)
        {
            var item = itemDatabase.Get(itemId);
            if (item is SeedData)
            {
                UserManager.Instance.SelectSeed(itemId);
                Debug.Log($"[Inventory] Seed selected: {itemId}");
            }
        }

        RefreshList();
    }

    private void ClearSelection()
    {
        _selectedItemId = null;
        _lastClickItemId = null;
        _lastClickTime = 0f;

        if (UserManager.Instance != null)
            UserManager.Instance.ClearSelectedSeed();
    }

    private static string Normalize(string s)
    {
        return string.IsNullOrWhiteSpace(s)
            ? null
            : s.Trim().ToLowerInvariant();
    }
}
