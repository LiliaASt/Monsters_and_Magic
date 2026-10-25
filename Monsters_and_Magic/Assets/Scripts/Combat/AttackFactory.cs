public static class AttackFactory
{
    public static IAttack Create(AttackStats stats)
    {
        switch (stats.type)
        {
            case AttackType.Projectile:
                return new ProjectileAttack();
            default:
                return new MeleeAttack();
        }
    }
}
