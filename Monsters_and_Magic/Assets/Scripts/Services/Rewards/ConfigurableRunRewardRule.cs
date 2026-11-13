using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "RunRewardRule",
    menuName = "Monsters and Magic/Run Rewards/Configurable Rule")]
public class ConfigurableRunRewardRule : RunRewardRule
{
    [SerializeField] private int baseAmount;
    [SerializeField] private int winBonus;
    [SerializeField] private int loseBonus;
    [SerializeField] private RunRewardFactor[] factors = Array.Empty<RunRewardFactor>();

    public override int GetAmount(RunResult result)
    {
        float amount = baseAmount;

        amount += result.State == RunState.Won
            ? winBonus
            : loseBonus;

        foreach (RunRewardFactor factor in factors)
            amount += factor.Evaluate(result);

        return Mathf.Max(0, Mathf.RoundToInt(amount));
    }
}
