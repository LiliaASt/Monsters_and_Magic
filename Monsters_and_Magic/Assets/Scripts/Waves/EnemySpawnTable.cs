using System;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class EnemySpawnEntry
{
    public EnemyConfig enemy;
    public AnimationCurve weight = AnimationCurve.Constant(1f, 100f, 1f);

    public float GetWeight(int waveNumber)
    {
        return Mathf.Max(0f, weight.Evaluate(waveNumber));
    }
}

[CreateAssetMenu(fileName = "EnemySpawnTable", menuName = "Monsters and Magic/Waves/Enemy Spawn Table")]
public class EnemySpawnTable : ScriptableObject
{
    public EnemySpawnEntry[] entries;

    public EnemyConfig Pick(int waveNumber)
    {
        float total = 0f;

        foreach (EnemySpawnEntry entry in entries)
            total += entry.GetWeight(waveNumber);

        if (total <= 0f)
            return entries[0].enemy;

        float roll = Random.value * total;

        foreach (EnemySpawnEntry entry in entries)
        {
            roll -= entry.GetWeight(waveNumber);

            if (roll <= 0f)
                return entry.enemy;
        }

        return entries[entries.Length - 1].enemy;
    }
}
