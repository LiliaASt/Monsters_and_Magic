using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(
    fileName = "RunRewards",
    menuName = "Monsters and Magic/Run Rewards/Provider")]
public class RunRewardProvider : ScriptableObject
{
    [SerializeField] private RunRewardRule[] rules = Array.Empty<RunRewardRule>();

    public RunRewards GetRewards(RunResult result)
    {
        Dictionary<ResourceConfig, int> amounts = new();

        foreach (RunRewardRule rule in rules)
        {
            int amount = rule.GetAmount(result);

            if (amount <= 0)
                continue;

            if (amounts.TryGetValue(rule.Resource, out int current))
                amounts[rule.Resource] = current + amount;
            else
                amounts.Add(rule.Resource, amount);
        }

        ResourceAmount[] rewards = amounts
            .Select(item => new ResourceAmount(item.Key, item.Value))
            .ToArray();

        return new RunRewards(rewards);
    }
}
