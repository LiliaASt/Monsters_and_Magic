using UnityEngine;

public class MeleeAttack : IAttack
{
    private IAttackSource source;
    private float cooldownTimer;

    public void Initialize(IAttackSource source)
    {
        this.source = source;
    }

    public void Tick()
    {
        cooldownTimer -= Time.deltaTime;

        if (cooldownTimer > 0f)
            return;

        if (source.TargetHealth == null)
            return;

        if (source.DistanceToTarget > source.Stats.range)
            return;

        source.TargetHealth.TakeDamage(source.Stats.damage);
        cooldownTimer = source.Stats.cooldown;
    }
}
