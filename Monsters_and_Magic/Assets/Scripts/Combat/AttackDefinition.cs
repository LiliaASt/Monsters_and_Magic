using UnityEngine;

public abstract class AttackDefinition : ScriptableObject
{
    public string id;
    public AttackStats[] ranks = { new AttackStats() };

    public int MaxRank => ranks.Length;

    public AttackStats GetStats(int rank)
    {
        return ranks[rank - 1];
    }

    public abstract IAttack Create();
}
