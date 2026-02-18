using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Farming Game/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<ItemData> items = new();

    private Dictionary<string, ItemData> _byId;

    private void OnEnable()
    {
        RebuildIndex();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RebuildIndex();
    }
#endif

    private void RebuildIndex()
    {
        // без учёта регистра
        _byId = new Dictionary<string, ItemData>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            if (item == null) continue;

            var id = item.itemId;
            if (string.IsNullOrWhiteSpace(id)) continue;

            id = id.Trim();
            _byId[id] = item;
        }
    }

    public ItemData Get(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        if (_byId == null || _byId.Count == 0)
            RebuildIndex();

        return _byId.TryGetValue(id.Trim(), out var data) ? data : null;
    }

    public string GetDisplayName(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return itemId;

        string normalized = Normalize(itemId);

        foreach (var item in items)
        {
            if (Normalize(item.itemId) == normalized)
                return item.itemName;
        }

        return itemId;
    }

    private static string Normalize(string s)
    {
        return s.Trim().ToLowerInvariant();
    }

}