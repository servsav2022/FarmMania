using System.Collections.Generic;
using UnityEngine;

public class QuestsPanelUI : MonoBehaviour
{
    [Header("Ссылки на UI")]
    [SerializeField] private Transform content;
    [SerializeField] private QuestItemView itemPrefab;

    private readonly List<QuestItemView> spawned = new();

    private void OnEnable()
    {
        var qm = QuestManagerMono.Instance;
        if (qm != null)
            qm.QuestsChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        var qm = QuestManagerMono.Instance;
        if (qm != null)
            qm.QuestsChanged -= Refresh;
    }

    public void Refresh()
    {
        ClearSpawned();

        var qm = QuestManagerMono.Instance;
        if (qm == null)
        {
            SpawnTextItem("Менеджер заданий не найден");
            return;
        }

        var list = qm.GetQuestsInOrder();
        if (list == null || list.Length == 0)
        {
            SpawnTextItem("Список заданий пуст");
            return;
        }

        bool hasAny = false;

        for (int i = 0; i < list.Length; i++)
        {
            var q = list[i];
            if (q == null || q.def == null)
                continue;

            hasAny = true;

            int t = q.def.target;
            if (t < 0) t = 0;

            string prefix = q.completed ? "✓ " : "";
            string line = $"{prefix}{q.def.title} ({q.current}/{t})";

            SpawnTextItem(line);
        }

        if (!hasAny)
            SpawnTextItem("Список заданий пуст");
    }

    private void SpawnTextItem(string title)
    {
        var item = Instantiate(itemPrefab, content);
        item.Bind(title, null);
        spawned.Add(item);
    }

    private void ClearSpawned()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null)
                Destroy(spawned[i].gameObject);
        }

        spawned.Clear();
    }
}