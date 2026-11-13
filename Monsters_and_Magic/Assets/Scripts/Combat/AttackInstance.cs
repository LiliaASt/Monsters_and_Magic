public class AttackInstance
{
    public AttackDefinition Definition { get; }
    public AttackStats Stats { get; private set; }
    public int Rank { get; private set; } = 1;

    public AttackInstance(AttackDefinition definition)
    {
        Definition = definition;
        Stats = definition.GetStats(Rank).Copy();
    }

    public bool TryUpgrade()
    {
        if (Rank >= Definition.MaxRank)
            return false;

        Rank++;
        Stats = Definition.GetStats(Rank).Copy();
        return true;
    }
}
