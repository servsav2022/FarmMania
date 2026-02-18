// BonusNpcInteractor.cs
using UnityEngine;

public class BonusNpcInteractor : MonoBehaviour
{
    [SerializeField] private float interactDistance = 1.2f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private Transform player;

    private void Start()
    {
        TryFindPlayer();
    }

    private void Update()
    {
        if (player == null)
        {
            TryFindPlayer();
            if (player == null) return;
        }

        float d = Vector2.Distance(player.position, transform.position);
        if (d > interactDistance)
            return;

        if (Input.GetKeyDown(interactKey))
        {
            if (QuestManagerMono.Instance == null)
            {
                Debug.LogWarning("BonusNpcInteractor: QuestManagerMono.Instance не найден.");
                return;
            }

            bool accepted = QuestManagerMono.Instance.TryAcceptBonusQuestFromNpc();

            // Если квест взят - NPC исчезает.
            // Если квест не взят (есть прошлый) - NPC остаётся и продолжает стоять оставшееся время.
            if (accepted)
                Destroy(gameObject);
        }
    }

    private void TryFindPlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null)
        {
            player = go.transform;
            return;
        }
    }
}