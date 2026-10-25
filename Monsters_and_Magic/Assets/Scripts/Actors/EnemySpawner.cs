using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyActor enemyPrefab;
    [SerializeField] private HeroActor hero;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxAlive = 10;
    [SerializeField] private float spawnRadius = 8f;

    private readonly List<EnemyActor> alive = new();
    private float timer;

    private void Update()
    {
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
    }

    private void HandleDied(EnemyActor enemy)
    {
        enemy.Died -= HandleDied;
        alive.Remove(enemy);
    }
}
