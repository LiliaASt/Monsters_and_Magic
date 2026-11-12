using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private HeroActor hero;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private float spawnMargin = 1.5f;
    [SerializeField] private float portalMinDistance = 2.5f;

    private readonly List<EnemyActor> alive = new();
    private readonly List<GameObject> portals = new();

    private GameObject portalPrefab;
    private float portalDelay;
    private int pendingCount;

    public int AliveCount => alive.Count;
    public int OccupiedCount => alive.Count + pendingCount;

    public event Action<EnemyActor> Spawned;

    private void Awake()
    {
        if (gameplayCamera == null)
            gameplayCamera = Camera.main;
    }

    public void Configure(WaveRules rules)
    {
        portalPrefab = rules.portalPrefab;
        portalDelay = rules.portalDelay;
    }

    public EnemyActor Spawn(EnemyConfig config)
    {
        return Spawn(config, GetOffscreenPosition(spawnMargin));
    }

    public int Spawn(SpawnGroupConfig group)
    {
        pendingCount += group.TotalCount;
        StartCoroutine(SpawnGroup(group));
        return group.TotalCount;
    }

    public void Stop()
    {
        StopAllCoroutines();
        pendingCount = 0;

        foreach (GameObject portal in portals)
            Destroy(portal);

        portals.Clear();
    }

    private IEnumerator SpawnGroup(SpawnGroupConfig group)
    {
        Vector2 center = GetPortalPosition(group.spread);
        GameObject portal = null;

        if (portalPrefab != null)
        {
            portal = Instantiate(portalPrefab, center, Quaternion.identity);
            portals.Add(portal);
        }

        yield return new WaitForSeconds(portalDelay);

        portals.Remove(portal);

        if (portal != null)
            Destroy(portal);

        foreach (SpawnGroupEntry entry in group.entries)
        {
            for (int i = 0; i < entry.count; i++)
            {
                Vector2 offset = Random.insideUnitCircle * group.spread;
                Spawn(entry.enemy, center + offset);
            }
        }

        pendingCount -= group.TotalCount;
    }

    private EnemyActor Spawn(EnemyConfig config, Vector2 position)
    {
        position = FindFreePosition(position);
        EnemyActor enemy = Instantiate(config.prefab, position, Quaternion.identity);
        enemy.Initialize(config, hero);
        enemy.Died += HandleDied;
        alive.Add(enemy);
        Spawned?.Invoke(enemy);
        return enemy;
    }

    /// <summary>
    /// Keeps enemies out of obstacle tiles when the world has them enabled.
    /// Tries small offsets around the requested point before giving up and
    /// using the original position (obstacles are optional and sparse).
    /// </summary>
    private Vector2 FindFreePosition(Vector2 position)
    {
        ChunkManager world = ChunkManager.Instance;

        if (world == null || !world.IsPositionBlocked(position))
            return position;

        for (int i = 0; i < 8; i++)
        {
            Vector2 candidate = position + (Vector2)(Quaternion.Euler(0f, 0f, i * 45f) * Vector2.right) * 1.5f;

            if (!world.IsPositionBlocked(candidate))
                return candidate;
        }

        return position;
    }

    private Vector2 GetOffscreenPosition(float margin)
    {
        GetBounds(out Vector2 min, out Vector2 max);
        float x = Random.Range(min.x, max.x);
        float y = Random.Range(min.y, max.y);

        switch (Random.Range(0, 4))
        {
            case 0: return new Vector2(min.x - margin, y);
            case 1: return new Vector2(max.x + margin, y);
            case 2: return new Vector2(x, min.y - margin);
            default: return new Vector2(x, max.y + margin);
        }
    }

    private Vector2 GetPortalPosition(float spread)
    {
        GetBounds(out Vector2 min, out Vector2 max);
        Vector2 padding = Vector2.Min(
            Vector2.one * (spread + 0.5f),
            (max - min) * 0.35f);
        min += padding;
        max -= padding;

        for (int i = 0; i < 12; i++)
        {
            Vector2 position = new(
                Random.Range(min.x, max.x),
                Random.Range(min.y, max.y));

            if (Vector2.Distance(position, hero.transform.position) >= portalMinDistance)
                return position;
        }

        return new Vector2(max.x, max.y);
    }

    private void GetBounds(out Vector2 min, out Vector2 max)
    {
        float depth = Mathf.Abs(hero.transform.position.z - gameplayCamera.transform.position.z);
        min = gameplayCamera.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
        max = gameplayCamera.ViewportToWorldPoint(new Vector3(1f, 1f, depth));
    }

    private void HandleDied(EnemyActor enemy)
    {
        enemy.Died -= HandleDied;
        alive.Remove(enemy);
    }
}
