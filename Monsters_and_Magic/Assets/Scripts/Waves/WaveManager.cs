using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class WaveManager : MonoBehaviour
{
    [SerializeField] private EnemySpawner spawner;

    private readonly WaveGenerator generator = new();
    private readonly Queue<SpawnGroupConfig> forcedGroups = new();

    private WaveRules rules;
    private WaveState wave;
    private int waveCount;
    private int waveNumber;
    private int spawned;
    private float elapsed;
    private float spawnTimer;
    private float transitionTimer;
    private bool transitioning;

    public WaveState CurrentWave => wave;
    public bool IsRunning { get; private set; }
    public bool IsCompleted { get; private set; }

    public event Action<WaveState> WaveStarted;
    public event Action<WaveState> WaveCompleted;
    public event Action Completed;

    public void Initialize(WaveRules rules, int waveCount)
    {
        this.rules = rules;
        this.waveCount = waveCount;
        waveNumber = 0;
        IsCompleted = false;
        spawner.Configure(rules);
        StartNextWave();
    }

    public void Stop()
    {
        IsRunning = false;
        transitioning = false;
    }

    private void Update()
    {
        if (transitioning)
        {
            transitionTimer -= Time.deltaTime;

            if (transitionTimer <= 0f)
                StartNextWave();

            return;
        }

        if (!IsRunning)
            return;

        elapsed += Time.deltaTime;
        spawnTimer -= Time.deltaTime;

        TrySpawn();

        if (CanCompleteWave())
            CompleteWave();
    }

    private void TrySpawn()
    {
        if (spawned >= wave.EnemyCount)
            return;

        if (spawner.OccupiedCount >= wave.MaxAlive)
            return;

        if (spawnTimer > 0f)
            return;

        if (forcedGroups.Count > 0)
        {
            SpawnForcedGroup();
            return;
        }

        if (Random.value < wave.GroupChance)
        {
            SpawnGroupConfig group = PickGroup();

            if (group != null && TrySpawnGroup(group))
            {
                ResetSpawnTimer();
                return;
            }
        }

        EnemyConfig enemy = wave.SpawnTable.Pick(wave.Number);
        spawner.Spawn(enemy);
        spawned++;
        ResetSpawnTimer();
    }

    private void SpawnForcedGroup()
    {
        SpawnGroupConfig group = forcedGroups.Peek();

        if (!TrySpawnGroup(group))
            return;

        forcedGroups.Dequeue();
        ResetSpawnTimer();
    }

    private bool TrySpawnGroup(SpawnGroupConfig group)
    {
        int remaining = wave.EnemyCount - spawned;
        int available = wave.MaxAlive - spawner.OccupiedCount;

        if (group.TotalCount > remaining)
            return false;

        if (group.TotalCount > available)
            return false;

        spawned += spawner.Spawn(group);
        return true;
    }

    private SpawnGroupConfig PickGroup()
    {
        float total = 0f;

        foreach (SpawnGroupConfig group in wave.Groups)
            total += group.GetWeight(wave.Number);

        if (total <= 0f)
            return null;

        float roll = Random.value * total;

        foreach (SpawnGroupConfig group in wave.Groups)
        {
            roll -= group.GetWeight(wave.Number);

            if (roll <= 0f)
                return group;
        }

        return wave.Groups[wave.Groups.Length - 1];
    }

    private bool CanCompleteWave()
    {
        return elapsed >= wave.Duration
            && spawned >= wave.EnemyCount
            && spawner.OccupiedCount == 0;
    }

    private void CompleteWave()
    {
        IsRunning = false;
        WaveCompleted?.Invoke(wave);

        if (waveCount > 0 && wave.Number >= waveCount)
        {
            IsCompleted = true;
            Completed?.Invoke();
            return;
        }

        transitioning = true;
        transitionTimer = rules.interWaveDelay;
    }

    private void StartNextWave()
    {
        transitioning = false;
        waveNumber++;
        wave = generator.Create(waveNumber, rules);
        elapsed = 0f;
        spawned = 0;
        spawnTimer = 0f;
        forcedGroups.Clear();

        foreach (SpawnGroupConfig group in wave.ForcedGroups)
            forcedGroups.Enqueue(group);

        IsRunning = true;
        WaveStarted?.Invoke(wave);
    }

    private void ResetSpawnTimer()
    {
        spawnTimer = wave.SpawnInterval;
    }
}
