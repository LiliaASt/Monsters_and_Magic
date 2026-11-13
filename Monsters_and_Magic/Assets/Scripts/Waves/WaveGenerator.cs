using UnityEngine;

public class WaveGenerator
{
    public WaveState Create(int waveNumber, WaveRules rules)
    {
        WaveState wave = new WaveState(
            waveNumber,
            Mathf.Max(0f, rules.duration.Evaluate(waveNumber)),
            Mathf.Max(0.05f, rules.spawnInterval.Evaluate(waveNumber)),
            Mathf.Max(1, Mathf.RoundToInt(rules.enemyCount.Evaluate(waveNumber))),
            Mathf.Max(1, Mathf.RoundToInt(rules.maxAlive.Evaluate(waveNumber))),
            Mathf.Clamp01(rules.groupChance.Evaluate(waveNumber)),
            rules.spawnTable,
            rules.groups);

        foreach (WaveModifier modifier in rules.modifiers)
        {
            if (modifier.Applies(waveNumber))
                modifier.Apply(wave);
        }

        return wave;
    }
}
