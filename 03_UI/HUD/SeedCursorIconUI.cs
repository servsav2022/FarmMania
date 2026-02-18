using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SeedCursorIconUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private Vector2 offset = new Vector2(16, -16);

    private string _lastSeedId;

    private void Update()
    {
        FollowCursor();
        UpdateIconIfNeeded();
    }

    private void FollowCursor()
    {
        if (Mouse.current == null)
            return;

        Vector2 pos = Mouse.current.position.ReadValue();
        transform.position = pos + offset;
    }

    private void UpdateIconIfNeeded()
    {
        string seedId = UserManager.Instance?.SelectedSeedItemId;

        if (string.IsNullOrEmpty(seedId))
        {
            iconImage.enabled = false;
            _lastSeedId = null;
            return;
        }

        if (seedId == _lastSeedId)
            return;

        var item = itemDatabase.Get(seedId) as SeedData;
        if (item == null || item.icon == null)
        {
            iconImage.enabled = false;
            return;
        }

        iconImage.sprite = item.icon;
        iconImage.enabled = true;
        _lastSeedId = seedId;
    }
}