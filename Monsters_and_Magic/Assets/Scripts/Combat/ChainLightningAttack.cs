using System.Collections.Generic;
using UnityEngine;

public class ChainLightningAttack : IAttack
{
    private readonly ChainLightningAttackDefinition definition;
    private IAttackSource source;
    private AttackInstance attack;
    private float cooldownTimer;

    public ChainLightningAttack(ChainLightningAttackDefinition definition)
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

        if (cooldownTimer > 0f || source.TargetHealth == null || source.DistanceToTarget > stats.range)
            return;

        IDamageable current = source.TargetOrigin.GetComponentInParent<IDamageable>();
        var visited = new HashSet<IDamageable>();
        Vector3 previous = source.Origin.position;

        for (int i = 0; i < definition.chainTargets; i++)
        {
            visited.Add(current);
            Vector3 position = ((Component)current).transform.position;

            ShowArc(previous, position);
            if (definition.hitEffect != null)
                VfxEffect.Spawn(definition.hitEffect, position);

            current.Health.TakeDamage(stats.damage);
            previous = position;
            current = FindNext(position, visited);

            if (current == null)
                break;
        }

        cooldownTimer = stats.cooldown;
    }

    private IDamageable FindNext(Vector3 position, HashSet<IDamageable> visited)
    {
        IDamageable nearest = null;
        float best = definition.chainRadius * definition.chainRadius;

        foreach (Collider2D collider in Physics2D.OverlapCircleAll(position, definition.chainRadius))
        {
            IDamageable candidate = CombatTargets.Get(collider, source.Team);

            if (candidate == null || visited.Contains(candidate))
                continue;

            float distance = (((Component)candidate).transform.position - position).sqrMagnitude;

            if (distance >= best)
                continue;

            nearest = candidate;
            best = distance;
        }

        return nearest;
    }

    private void ShowArc(Vector3 from, Vector3 to)
    {
        if (definition.arcPrefab == null)
            return;

        VfxEffect arc = VfxEffect.Spawn(definition.arcPrefab, from);
        LightningArcAnimator animator = arc.GetComponent<LightningArcAnimator>();
        if (animator != null)
            animator.Initialize(from, to, definition.arcSegments, definition.arcJitter);

        arc.PlayFor(definition.arcLifetime);
    }
}
