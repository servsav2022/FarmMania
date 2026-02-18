using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TimedNpcSpawner : MonoBehaviour
{
    [Header("Tilemap")]
    [SerializeField] private Tilemap decorationTilemap;

    [Header("NPC")]
    [SerializeField] private GameObject npcPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnIntervalSeconds = 60f;
    [SerializeField] private float npcLifetimeSeconds = 15f;
    [SerializeField] private int maxSpawnAttempts = 60;

    [Header("Spawn Validation")]
    [SerializeField] private LayerMask blockingMask;
    [SerializeField] private float blockCheckRadius = 0.35f;

    private GameObject currentNpc;

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnIntervalSeconds);

            if (currentNpc != null)
                continue;

            Vector3 spawnPos;
            bool ok = TryGetRandomSpawnPosition(out spawnPos);

            if (!ok)
                continue;

            currentNpc = Instantiate(npcPrefab, spawnPos, Quaternion.identity);

            var timed = currentNpc.GetComponent<TimedNpcDespawn>();
            if (timed != null)
                timed.Init(npcLifetimeSeconds, OnNpcExpired);
            else
                StartCoroutine(FallbackDespawn(npcLifetimeSeconds));
        }
    }

    private IEnumerator FallbackDespawn(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        OnNpcExpired();
    }

    private void OnNpcExpired()
    {
        if (currentNpc != null)
            Destroy(currentNpc);

        currentNpc = null;
    }

    private bool TryGetRandomSpawnPosition(out Vector3 worldPos)
    {
        worldPos = Vector3.zero;

        if (decorationTilemap == null)
            return false;

        var bounds = decorationTilemap.cellBounds;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            int x = Random.Range(bounds.xMin, bounds.xMax);
            int y = Random.Range(bounds.yMin, bounds.yMax);

            var cell = new Vector3Int(x, y, 0);

            if (!decorationTilemap.HasTile(cell))
                continue;

            Vector3 candidate = decorationTilemap.GetCellCenterWorld(cell);

            if (Physics2D.OverlapCircle(candidate, blockCheckRadius, blockingMask) != null)
                continue;

            worldPos = candidate;
            return true;
        }

        return false;
    }
}
