using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyActor : MonoBehaviour, IAttackSource
{
    [SerializeField] private EnemyConfig config;
    [SerializeField] private EnemySet enemies;
    [SerializeField] private float separationRadius = 0.9f;

    public EnemyConfig Config => config;
    public HeroActor Target { get; set; }
    public bool IsDead => enemy.Health.IsDead;
    public Health Health => enemy.Health;
    public Transform Origin => transform;
    public Transform TargetOrigin => Target == null ? null : Target.transform;
    public Health TargetHealth => Target == null ? null : Target.Hero.Health;
    public AttackStats Stats => config.attack;
    public float DistanceToTarget => TargetOrigin == null
        ? float.MaxValue
        : Vector2.Distance(Origin.position, TargetOrigin.position);

    public event Action<EnemyActor> Died;

    private Enemy enemy;
    private Rigidbody2D rb;
    private IAttack attack;

    private void Awake()
    {
        enemy = new Enemy(config);
        rb = GetComponent<Rigidbody2D>();
        attack = AttackFactory.Create(config.attack);
        attack.Initialize(this);
        enemy.Health.Died += HandleDied;
        enemy.Health.Damaged += HandleDamaged;
        enemies.Add(this);
    }

    private void FixedUpdate()
    {
        if (Target == null || IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = ((Vector2)Target.transform.position - rb.position).normalized;
        rb.linearVelocity = direction * enemy.MoveSpeed + Separate();
        rb.MoveRotation(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        attack.Tick();
    }

    private Vector2 Separate()
    {
        Vector2 separation = Vector2.zero;

        foreach (EnemyActor other in enemies.Items)
        {
            if (other == null || other == this)
                continue;

            Vector2 toOther = (Vector2)other.transform.position - rb.position;
            float distance = toOther.magnitude;

            if (distance >= separationRadius || distance <= 0f)
                continue;

            separation -= toOther / distance * (separationRadius - distance);
        }

        return separation;
    }

    private void HandleDamaged(float amount)
    {
        Debug.Log($"{config.displayName}: -{amount} HP ({enemy.Health.Current}/{enemy.Health.Max})");
    }

    private void HandleDied()
    {
        Died?.Invoke(this);
        enemies.Remove(this);
        Destroy(gameObject);
    }
}
