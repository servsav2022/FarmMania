using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryListRowUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Image selectionHighlight;

    private string _itemId;
    private System.Action<string> _onClick;

    public void Bind(
        string itemId,
        string displayName,
        int amount,
        int price,
        bool selected,
        System.Action<string> onClick
    )
    {
        _itemId = itemId;
        _onClick = onClick;

        // Name
        if (nameText != null)
            nameText.text = displayName ?? string.Empty;

        // Amount
        if (amountText != null)
            amountText.text = $"x{amount}";

        // Price
        if (priceText != null)
        {
            priceText.gameObject.SetActive(price > 0);
            if (price > 0)
                priceText.text = price + " Монет";
        }

        // Selection highlight
        if (selectionHighlight != null)
            selectionHighlight.enabled = selected;

        // Click
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClickInternal);
        }
    }

    private void OnClickInternal()
    {
        Debug.Log($"[InventoryRow] Click: {_itemId}");
        _onClick?.Invoke(_itemId);
    }
}