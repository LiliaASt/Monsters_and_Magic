using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyActor : MonoBehaviour, IAttackSource
{
    [SerializeField] private EnemyConfig config;
    [SerializeField] private EnemySet enemies;

    public EnemyConfig Config => config;
    public HeroActor Target { get; set; }
    public bool IsDead => enemy.Health.IsDead;
    public Health Health => enemy.Health;
    public Transform Origin => transform;
    public Health TargetHealth => Target?.Hero.Health;
    public AttackStats Stats => config.attack;
    public float DistanceToTarget => TargetHealth == null
        ? float.MaxValue
        : Vector2.Distance(Origin.position, TargetHealth.transform.position);

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
        rb.linearVelocity = direction * enemy.MoveSpeed;
        rb.MoveRotation(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        attack.Tick();
    }

    private void HandleDied()
    {
        Died?.Invoke(this);
        enemies.Remove(this);
        Destroy(gameObject);
    }
}
