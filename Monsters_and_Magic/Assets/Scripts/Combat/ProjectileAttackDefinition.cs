using UnityEngine;

public enum ProjectileMotion
{
    Curve,
    Homing,
    Boomerang
}

public enum ProjectileImpact
{
    Single,
    Piercing,
    Explosion,
    TimedExplosion
}

[CreateAssetMenu(fileName = "ProjectileAttack", menuName = "Monsters and Magic/Attacks/Projectile")]
public class ProjectileAttackDefinition : AttackDefinition
{
    public Projectile prefab;
    public ProjectileMotion motion;
    public ProjectileImpact impact;

    [Min(0.01f)] public float duration = 3f;
    [Min(0f)] public float speed = 8f;
    [Min(0.1f)] public float maxDistance = 6f;
    [Min(0f)] public float maxLateralOffset = 0.6f;

    public AnimationCurve speedOverLifetime =
        AnimationCurve.Constant(0f, 1f, 1f);

    public AnimationCurve lateralOffset =
        AnimationCurve.Constant(0f, 1f, 0f);

    [Tooltip("Curve projectiles blend into a direct homing push near the target (missiles: yes, fireballs: no).")]
    public bool curveHoming;

    public AnimationCurve homingTurnSpeed =
        AnimationCurve.Constant(0f, 1f, 360f);

    [Range(0.1f, 0.95f)] public float returnAt = 0.5f;
    [Min(0f)] public float returnSpeed = 12f;
    [Min(1)] public int maxHits = 1;
    [Min(0f)] public float explosionRadius = 2f;

    [Tooltip("TimedExplosion: damage dealt when flying through an enemy.")]
    [Min(0f)] public float pierceDamage = 8f;

    [Tooltip("TimedExplosion: damage dealt by the final explosion.")]
    [Min(0f)] public float explosionDamage = 10f;
    public GameObject flightEffect;
    public GameObject hitEffect;

    public override IAttack Create()
    {
        return new ProjectileAttack(this);
    }
}
