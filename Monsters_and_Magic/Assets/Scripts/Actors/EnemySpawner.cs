using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyActor enemyPrefab;
    [SerializeField] private HeroActor hero;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxAlive = 10;
    [SerializeField] private float spawnRadius = 8f;

    public event Action<EnemyActor> Spawned;
    public int AliveCount => alive.Count;

    private readonly List<EnemyActor> alive = new();
    private float timer;
    private bool spawning = true;

    public void Stop()
    {
        spawning = false;
    }

    private void Update()
    {
        if (!spawning)
            return;

        timer -= Time.deltaTime;

        if (timer > 0f)
            return;

        if (hero == null || hero.IsDead)
            return;

        if (alive.Count >= maxAlive)
            return;

        Spawn();
        timer = spawnInterval;
    }

    private void Spawn()
    {
        Vector2 offset = Random.insideUnitCircle.normalized * spawnRadius;

        EnemyActor enemy = Instantiate(
            enemyPrefab,
            hero.transform.position + (Vector3)offset,
            Quaternion.identity);

        enemy.Target = hero;
        enemy.Died += HandleDied;
        alive.Add(enemy);
        Spawned?.Invoke(enemy);
    }

    private void HandleDied(EnemyActor enemy)
    {
        enemy.Died -= HandleDied;
        alive.Remove(enemy);
    }
}
