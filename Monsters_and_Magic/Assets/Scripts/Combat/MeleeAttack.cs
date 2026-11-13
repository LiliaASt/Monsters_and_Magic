using UnityEngine;

public class MeleeAttack : IAttack
{
    private readonly MeleeAttackDefinition definition;
    private IAttackSource source;
    private AttackInstance attack;
    private float cooldownTimer;

    public MeleeAttack(MeleeAttackDefinition definition)
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

        source.TargetHealth.TakeDamage(stats.damage);

        if (definition != null && definition.hitEffect != null)
            VfxEffect.Spawn(definition.hitEffect, source.TargetOrigin.position);

        cooldownTimer = stats.cooldown;
    }
}
