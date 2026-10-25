using UnityEngine;

public class ProjectileAttack : IAttack
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

        Fire();
        cooldownTimer = source.Stats.cooldown;
    }

    private void Fire()
    {
        Projectile projectile = Object.Instantiate(
            source.Stats.projectilePrefab,
            source.Origin.position,
            Quaternion.identity);

        projectile.Launch(
            source.TargetHealth.transform.position,
            source.Stats.damage,
            source.Stats.projectileSpeed);
    }
}
