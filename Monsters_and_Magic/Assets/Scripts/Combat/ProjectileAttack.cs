using UnityEngine;

public class ProjectileAttack : IAttack
{
    private readonly ProjectileAttackDefinition definition;
    private IAttackSource source;
    private AttackInstance attack;
    private float cooldownTimer;

    public ProjectileAttack(ProjectileAttackDefinition definition)
    {
        this.definition = definition;
    }

    public void Initialize(IAttackSource source, AttackInstance attack)
    {
        this.source = source;
        this.attack = attack;
    }

    public void Tick()
    {
        cooldownTimer -= Time.deltaTime;
        AttackStats stats = attack.Stats;

        if (cooldownTimer > 0f || source.TargetHealth == null)
            return;

        if (source.DistanceToTarget > stats.range)
            return;

        Projectile instance = Object.Instantiate(
            definition.prefab,
            source.Origin.position,
            source.Origin.rotation);

        instance.Launch(
            source.Origin,
            source.TargetOrigin,
            source.Team,
            stats.damage,
            stats.range,
            definition);

        cooldownTimer = stats.cooldown;
    }
}
