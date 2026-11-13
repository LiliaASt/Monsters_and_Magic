using System;
using UnityEngine;

[Serializable]
public class RunRewardFactor
{
    [SerializeField] private RunRewardMetric metric;
    [SerializeField] private AnimationCurve amount = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    public float Evaluate(RunResult result)
    {
        return amount.Evaluate(GetValue(result));
    }

    private float GetValue(RunResult result)
    {
        switch (metric)
        {
            case RunRewardMetric.Kills:
                return result.Kills;

            case RunRewardMetric.Xp:
                return result.Xp;

            default:
                return result.Elapsed;
        }
    }
}
