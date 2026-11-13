using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyActor : MonoBehaviour, IAttackSource, IDamageable
{
    [SerializeField] private EnemySet enemies;
    [SerializeField] private WorldHealthBarController healthBar;
    [SerializeField] private DamagePopupPresenter damagePopups;
    [SerializeField] private float separationRadius = 0.9f;
    [SerializeField] private float despawnDistance = 32f;
    [SerializeField] private XPOrb orbPrefab;

    public EnemyConfig Config { get; private set; }
    public HeroActor Target { get; private set; }
    public bool IsDead => enemy.Health.IsDead;
    public Health Health => enemy.Health;
    public CombatTeam Team => CombatTeam.Enemy;
    public Transform Origin => transform;
    public Transform TargetOrigin => Target.transform;
    public Health TargetHealth => Target.Health;
    public float DistanceToTarget => Vector2.Distance(Origin.position, TargetOrigin.position);

    public event Action<EnemyActor> Died;

    private Enemy enemy;
    private Rigidbody2D rb;
    private IAttack attack;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(EnemyConfig config, HeroActor target)
    {
        Config = config;
        Target = target;
        enemy = new Enemy(config);

        attack = config.attack.Create();
        attack.Initialize(this, new AttackInstance(config.attack));

        enemy.Health.Died += HandleDied;
        enemy.Health.Damaged += HandleDamaged;

        enemies.Add(this);
        healthBar.Bind(enemy.Health, FindVisualSprite());

        if (damagePopups != null)
            damagePopups.Bind(enemy.Health);
    }

    private void FixedUpdate()
    {
        if (IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = ((Vector2)Target.transform.position - rb.position).normalized;

        rb.linearVelocity = direction * enemy.MoveSpeed + Separate();
        attack.Tick();
    }

    private void Update()
    {
        // Survivor rule: enemies left far behind free up the alive budget so
        // the spawner keeps pressure around the hero wherever he runs.
        if (IsDead || Target == null)
            return;

        if (Vector2.Distance(Origin.position, Target.Origin.position) > despawnDistance)
        {
            enemies.Remove(this);
            Destroy(gameObject);
        }
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

    }

    private SpriteRenderer FindVisualSprite()
    {
        // The billboard sprite lives in the Visual child since the 2.5D pass;
        // fall back to any sprite for older prefabs.
        Transform visual = transform.Find("Visual");
        return visual != null
            ? visual.GetComponent<SpriteRenderer>()
            : GetComponentInChildren<SpriteRenderer>();
    }

    private void HandleDied()
    {
        if (Config.deathEffect != null)
            VfxEffect.Spawn(Config.deathEffect, transform.position);

        if (orbPrefab != null && Config.xpReward > 0)
        {
            XPOrb orb = Instantiate(orbPrefab, transform.position, Quaternion.identity);
            orb.Initialize(Config.xpReward);
            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            orb.Scatter(direction, UnityEngine.Random.Range(1.5f, 3.5f));
        }

        Died?.Invoke(this);
        enemies.Remove(this);
        Destroy(gameObject);
    }
}
