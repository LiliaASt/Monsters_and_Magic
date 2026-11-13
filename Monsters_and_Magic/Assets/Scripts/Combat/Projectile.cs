using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    private readonly HashSet<IDamageable> hitTargets = new();

    private Rigidbody2D body;
    private ProjectileAttackDefinition config;
    private Transform owner;
    private Transform target;
    private Vector2 origin;
    private Vector2 forward;
    private Vector2 lateral;

    private float damage;
    private float range;
    private float elapsed;
    private float forwardDistance;
    private float travelled;
    private int hitCount;
    private CombatTeam ownerTeam;
    private bool launched;
    private bool returning;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    public void Launch(
        Transform owner,
        Transform target,
        CombatTeam ownerTeam,
        float damage,
        float range,
        ProjectileAttackDefinition config)
    {
        if (body == null)
            body = GetComponent<Rigidbody2D>();

        this.owner = owner;
        this.target = target;
        this.damage = damage;
        this.config = config;
        this.range = Mathf.Min(range, config.maxDistance);

        origin = body.position;
        forward = ((Vector2)target.position - origin).normalized;
        lateral = new Vector2(-forward.y, forward.x);
        this.ownerTeam = ownerTeam;

        if (config.flightEffect != null)
        {
            GameObject visual = Instantiate(config.flightEffect, transform.position, Quaternion.identity);
            visual.transform.SetParent(transform, true);
        }

        body.linearVelocity = Vector2.zero;
        body.MoveRotation(Angle(forward));
        launched = true;
    }

    private void FixedUpdate()
    {
        if (!launched)
            return;

        if (elapsed >= config.duration)
        {
            EndFlight();
            return;
        }

        elapsed += Time.fixedDeltaTime;

        if (config.motion == ProjectileMotion.Boomerang && returning)
        {
            ReturnToOwner(Time.fixedDeltaTime);
            return;
        }

        if (travelled >= range)
        {
            if (config.motion == ProjectileMotion.Boomerang)
                returning = true;
            else
                EndFlight();

            return;
        }

        float progress = Mathf.Clamp01(elapsed / config.duration);

        switch (config.motion)
        {
            case ProjectileMotion.Homing:
                MoveHoming(progress, Time.fixedDeltaTime);
                break;
            case ProjectileMotion.Boomerang:
                if (progress >= config.returnAt)
                    returning = true;
                else
                    MoveCurve(progress / config.returnAt, Time.fixedDeltaTime);
                break;
            default:
                MoveCurve(progress, Time.fixedDeltaTime);
                break;
        }
    }

    private void MoveCurve(float progress, float deltaTime)
    {
        float speed = config.speed * Mathf.Max(0f, config.speedOverLifetime.Evaluate(progress));
        forwardDistance = Mathf.Min(range, forwardDistance + speed * deltaTime);

        float side = Mathf.Clamp(
            config.lateralOffset.Evaluate(progress),
            -config.maxLateralOffset,
            config.maxLateralOffset);

        Vector2 destination = origin + forward * forwardDistance + lateral * side;

        // Optional delivery assist for guided curve projectiles only - a
        // fireball must keep flying its plain arc.
        if (config.curveHoming && target != null)
        {
            Vector2 toTarget = (Vector2)target.position - body.position;
            float distance = toTarget.magnitude;

            if (distance < 2.5f)
            {
                float blend = 1f - distance / 2.5f;
                destination = Vector2.Lerp(destination, (Vector2)target.position, blend);
            }
        }

        MoveTo(destination);
    }

    private void MoveHoming(float progress, float deltaTime)
    {
        float angle = body.rotation;

        if (target != null)
        {
            Vector2 direction = (Vector2)target.position - body.position;
            float desired = Angle(direction);
            angle = Mathf.MoveTowardsAngle(
                angle,
                desired,
                config.homingTurnSpeed.Evaluate(progress) * deltaTime);
        }

        float radians = angle * Mathf.Deg2Rad;
        Vector2 forwardDirection = new(Mathf.Cos(radians), Mathf.Sin(radians));
        float speed = config.speed * Mathf.Max(0f, config.speedOverLifetime.Evaluate(progress));
        float step = Mathf.Min(speed * deltaTime, range - travelled);

        body.MoveRotation(angle);
        MoveTo(body.position + forwardDirection * step);
    }

    private void ReturnToOwner(float deltaTime)
    {
        if (owner == null)
        {
            Finish();
            return;
        }

        Vector2 destination = owner.position;
        Vector2 position = Vector2.MoveTowards(body.position, destination, config.returnSpeed * deltaTime);
        MoveTo(position, false);

        if (Vector2.Distance(position, destination) <= 0.2f)
            Finish();
    }

    private void MoveTo(Vector2 position, bool countDistance = true)
    {
        Vector2 direction = position - body.position;

        if (direction.sqrMagnitude > 0f)
            body.MoveRotation(Angle(direction));

        if (countDistance)
            travelled += direction.magnitude;

        body.MovePosition(position);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!launched)
            return;

        IDamageable damageable = CombatTargets.Get(other, ownerTeam);

        if (damageable == null || damageable.Health.IsDead || !hitTargets.Add(damageable))
            return;

        switch (config.impact)
        {
            case ProjectileImpact.Piercing:
                damageable.Health.TakeDamage(damage);
                hitCount++;
                SpawnHitEffect();
                if (hitCount >= config.maxHits)
                    Finish();
                break;
            case ProjectileImpact.Explosion:
                Explode();
                break;
            case ProjectileImpact.TimedExplosion:
                damageable.Health.TakeDamage(config.pierceDamage);
                SpawnHitEffect();
                break;
            default:
                launched = false;
                damageable.Health.TakeDamage(damage);
                SpawnHitEffect();
                Destroy(gameObject);
                break;
        }
    }

    private void Explode()
    {
        launched = false;
        var affected = new HashSet<IDamageable>();

        foreach (Collider2D collider in Physics2D.OverlapCircleAll(
            transform.position, config.explosionRadius))
        {
            IDamageable damageable = CombatTargets.Get(collider, ownerTeam);

            if (damageable == null || damageable.Health.IsDead || !affected.Add(damageable))
                continue;

            damageable.Health.TakeDamage(
                config.impact == ProjectileImpact.TimedExplosion
                    ? config.explosionDamage
                    : damage);
        }

        SpawnHitEffect();
        Destroy(gameObject);
    }

    private void SpawnHitEffect()
    {
        if (config.hitEffect != null)
            VfxEffect.Spawn(config.hitEffect, transform.position);
    }

    private void EndFlight()
    {
        if (config.impact == ProjectileImpact.TimedExplosion)
            Explode();
        else
            Finish();
    }

    private void Finish()
    {
        launched = false;
        Destroy(gameObject);
    }

    private static float Angle(Vector2 direction)
    {
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }
}
