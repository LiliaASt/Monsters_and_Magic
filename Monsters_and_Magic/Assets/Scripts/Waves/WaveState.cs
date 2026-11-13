using System.Collections.Generic;

public class WaveState
{
    public int Number { get; }
    public float Duration { get; set; }
    public float SpawnInterval { get; set; }
    public int EnemyCount { get; set; }
    public int MaxAlive { get; set; }
    public float GroupChance { get; set; }
    public EnemySpawnTable SpawnTable { get; }
    public SpawnGroupConfig[] Groups { get; }
    public List<SpawnGroupConfig> ForcedGroups { get; } = new();

    public WaveState(
        int number,
        float duration,
        float spawnInterval,
        int enemyCount,
        int maxAlive,
        float groupChance,
        EnemySpawnTable spawnTable,
        SpawnGroupConfig[] groups)
    {
        Number = number;
        Duration = duration;
        SpawnInterval = spawnInterval;
        EnemyCount = enemyCount;
        MaxAlive = maxAlive;
        GroupChance = groupChance;
        SpawnTable = spawnTable;
        Groups = groups;
    }
}
