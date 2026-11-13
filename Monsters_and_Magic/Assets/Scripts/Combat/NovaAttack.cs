using System.Collections.Generic;
using UnityEngine;

public class NovaAttack : IAttack
{
    private readonly float radius;
    private readonly GameObject castEffect;
    private IAttackSource source;
    private AttackInstance attack;
    private float cooldownTimer;

    public NovaAttack(float radius, GameObject castEffect)
    {
        this.radius = radius;
        this.castEffect = castEffect;
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

        if (cooldownTimer > 0f || source.TargetHealth == null || source.DistanceToTarget > radius)
            return;

        var hit = new HashSet<IDamageable>();

        foreach (Collider2D collider in Physics2D.OverlapCircleAll(source.Origin.position, radius))
        {
            IDamageable target = CombatTargets.Get(collider, source.Team);

            if (target == null || !hit.Add(target))
                continue;

            target.Health.TakeDamage(stats.damage);
        }

        if (hit.Count == 0)
            return;

        if (castEffect != null)
            VfxEffect.Spawn(castEffect, source.Origin.position);

        cooldownTimer = stats.cooldown;
    }
}
