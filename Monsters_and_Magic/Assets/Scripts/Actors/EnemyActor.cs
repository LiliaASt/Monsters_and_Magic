using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyActor : MonoBehaviour
{
    [SerializeField] private EnemyConfig config;

    public EnemyConfig Config => config;
    public HeroActor Target { get; set; }

    public event Action<EnemyActor> Died;

    private Enemy enemy;
    private Rigidbody2D rb;

    private void Awake()
    {
        enemy = new Enemy(config);
        rb = GetComponent<Rigidbody2D>();
        enemy.Health.Died += HandleDied;
    }

    private void FixedUpdate()
    {
        if (Target == null || enemy.Health.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = ((Vector2)Target.transform.position - rb.position).normalized;
        rb.linearVelocity = direction * enemy.MoveSpeed;
        rb.MoveRotation(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (Target == null || collision.gameObject != Target.gameObject)
            return;

        Target.Hero.Health.TakeDamage(config.damage);
    }

    private void HandleDied()
    {
        Died?.Invoke(this);
        Destroy(gameObject);
    }
}
