using System.Text;
using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text inventoryText;

    [Header("DB")]
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("Refresh")]
    [SerializeField] private float refreshInterval = 0.2f;
    [SerializeField] private UnityEngine.UI.Button loadButton;

    private float _timer;

    private void Awake()
    {
        Refresh();
        UpdateLoadButtonState();
    }

    private void Update()
    {
        _timer += Time.unscaledDeltaTime;
        if (_timer >= refreshInterval)
        {
            _timer = 0f;
            Refresh();
        }
    }

    private void Refresh()
    {
        var save = UserManager.Instance?.CurrentSave;

        // 💰 Деньги
        if (moneyText != null)
        {
            int money = save != null ? save.money : 0;
            moneyText.text = $": {money}";
        }

        // 🎒 Инвентарь
        if (inventoryText != null)
            inventoryText.text = BuildInventoryText(save);
        UpdateLoadButtonState();
    }

    private string BuildInventoryText(GameSaveData save)
    {
        if (save == null || save.inventory == null || save.inventory.Count == 0)
            return "Inventory:\n-";

        var sb = new StringBuilder();
        sb.AppendLine("Inventory:");

        foreach (var entry in save.inventory)
        {
            if (entry == null || entry.amount <= 0)
                continue;

            string id = entry.itemId?.Trim() ?? "-";
            string name = itemDatabase != null
                ? itemDatabase.GetDisplayName(id)
                : id;

            sb.AppendLine($"- {name} x{entry.amount}");
        }

        return sb.ToString();
    }
    
    private void UpdateLoadButtonState()
    {
        if (loadButton == null)
            return;

        var um = UserManager.Instance;

        loadButton.interactable =
            um != null &&
            um.HasAnyManualSave();
    }

}