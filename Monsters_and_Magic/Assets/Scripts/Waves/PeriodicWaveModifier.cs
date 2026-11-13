using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PeriodicWaveModifier", menuName = "Monsters and Magic/Waves/Modifiers/Periodic")]
public class PeriodicWaveModifier : WaveModifier
{
    [Min(1)]
    public int firstWave = 1;

    [Min(1)]
    public int interval = 1;

    public float durationMultiplier = 1f;
    public float spawnIntervalMultiplier = 1f;
    public float enemyCountMultiplier = 1f;
    public float maxAliveMultiplier = 1f;
    public float groupChanceBonus;
    public SpawnGroupConfig[] forcedGroups = Array.Empty<SpawnGroupConfig>();

    public override bool Applies(int waveNumber)
    {
        return waveNumber >= firstWave
            && (waveNumber - firstWave) % interval == 0;
    }

    public override void Apply(WaveState wave)
    {
        wave.Duration *= durationMultiplier;
        wave.SpawnInterval *= spawnIntervalMultiplier;
        wave.EnemyCount = Mathf.CeilToInt(wave.EnemyCount * enemyCountMultiplier);
        wave.MaxAlive = Mathf.CeilToInt(wave.MaxAlive * maxAliveMultiplier);
        wave.GroupChance = Mathf.Clamp01(wave.GroupChance + groupChanceBonus);

        foreach (SpawnGroupConfig group in forcedGroups)
        {
            wave.ForcedGroups.Add(group);
            wave.EnemyCount = Mathf.Max(wave.EnemyCount, group.TotalCount);
            wave.MaxAlive = Mathf.Max(wave.MaxAlive, group.TotalCount);
        }
    }
}
