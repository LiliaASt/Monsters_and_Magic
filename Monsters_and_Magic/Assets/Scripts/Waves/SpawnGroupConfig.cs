using System;
using UnityEngine;

[Serializable]
public class SpawnGroupEntry
{
    public EnemyConfig enemy;

    [Min(1)]
    public int count = 1;
}

[CreateAssetMenu(fileName = "SpawnGroup", menuName = "Monsters and Magic/Waves/Spawn Group")]
public class SpawnGroupConfig : ScriptableObject
{
    public SpawnGroupEntry[] entries;
    public AnimationCurve weight = AnimationCurve.Constant(1f, 100f, 1f);
    public float spread = 1f;

    public int TotalCount
    {
        get
        {
            int result = 0;

            foreach (SpawnGroupEntry entry in entries)
                result += entry.count;

            return result;
        }
    }

    public float GetWeight(int waveNumber)
    {
        return Mathf.Max(0f, weight.Evaluate(waveNumber));
    }
}
