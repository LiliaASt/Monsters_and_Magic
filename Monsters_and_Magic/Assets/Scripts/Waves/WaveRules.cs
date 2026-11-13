using System;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveRules", menuName = "Monsters and Magic/Waves/Wave Rules")]
public class WaveRules : ScriptableObject
{
    public AnimationCurve duration = AnimationCurve.Linear(1f, 30f, 10f, 60f);
    public AnimationCurve spawnInterval = AnimationCurve.Linear(1f, 2f, 10f, 0.5f);
    public AnimationCurve enemyCount = AnimationCurve.Linear(1f, 10f, 10f, 50f);
    public AnimationCurve maxAlive = AnimationCurve.Linear(1f, 5f, 10f, 15f);
    public AnimationCurve groupChance = AnimationCurve.Linear(1f, 0f, 10f, 0.4f);
    public float interWaveDelay = 2f;
    public GameObject portalPrefab;
    [Min(0f)] public float portalDelay = 1.2f;
    public EnemySpawnTable spawnTable;
    public SpawnGroupConfig[] groups = Array.Empty<SpawnGroupConfig>();
    public WaveModifier[] modifiers = Array.Empty<WaveModifier>();
}
