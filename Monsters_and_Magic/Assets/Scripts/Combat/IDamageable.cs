public enum CombatTeam
{
    Player,
    Enemy
}

public interface IDamageable
{
    Health Health { get; }
    CombatTeam Team { get; }
}
