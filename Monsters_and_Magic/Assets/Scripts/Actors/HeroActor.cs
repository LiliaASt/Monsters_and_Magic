using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody2D))]
public class HeroActor : MonoBehaviour, IAttackSource, IDamageable
{
    public static HeroActor Instance { get; private set; }

    [SerializeField] private Vector2Channel move;
    [SerializeField] private EnemySet enemies;
    [SerializeField] private WorldHealthBarController healthBar;

    public Hero Hero { get; private set; }
    public Health Health { get; private set; }
    public CombatTeam Team => CombatTeam.Player;
    public bool IsDead => Health.IsDead;
    public Transform Origin => transform;
    public Transform TargetOrigin => target == null ? null : target.transform;
    public Health TargetHealth => target == null ? null : target.Health;
    public float DistanceToTarget => TargetOrigin == null
        ? float.MaxValue
        : Vector2.Distance(Origin.position, TargetOrigin.position);

    private readonly List<IAttack> attacks = new();
    private Rigidbody2D rb;
    private InputAction moveAction;
    private Run run;
    private Camera gameplayCamera;
    private Vector2 touchDirection;
    private Vector2 direction;
    private EnemyActor target;

    private void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        // The hero must physically collide with world props; enemy damage
        // is distance-based, so a trigger collider is not needed here.
        GetComponent<Collider2D>().isTrigger = false;
        // Pushing against a static collider would spin the body otherwise.
        rb.freezeRotation = true;

        // Physics moves the body at FixedUpdate rate (50 Hz) while rendering
        // runs faster; without interpolation the sprite stutters between
        // physics steps.
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        moveAction = GetComponent<PlayerInput>().actions["Move"];
        gameplayCamera = Camera.main;
    }

    public void Initialize(Hero hero, Run run)
    {
        Hero = hero;
        this.run = run;
        Health = new Health(hero.MaxHealth, hero.RegenPerSec);

        healthBar.Bind(Health, FindVisualSprite());

        foreach (AttackInstance attack in run.Attacks)
            AddAttack(attack);

        run.AttackAdded += AddAttack;

        Health.Damaged += HandleHeroDamaged;
        Health.Restored += HandleHeroRestored;
    }

    private void AddAttack(AttackInstance instance)
    {
        IAttack attack = instance.Definition.Create();
        attack.Initialize(this, instance);
        attacks.Add(attack);
    }

    private void OnEnable()
    {
        move.Subscribe(HandleTouch);
    }

    private void OnDisable()
    {
        move.Unsubscribe(HandleTouch);
    }

    private void OnDestroy()
    {
        if (run != null)
            run.AttackAdded -= AddAttack;
    }

    private void Update()
    {
        Health.Tick(Time.deltaTime);
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

    public event System.Action<int> XpCollected;


    public void GainXP(int amount)
    {
        XpCollected?.Invoke(amount);
    }

    private void FixedUpdate()
    {
        if (Health.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 keyboard = moveAction.ReadValue<Vector2>();
        Vector2 input = touchDirection.sqrMagnitude > 0f ? touchDirection : keyboard;

        direction = Vector2.ClampMagnitude(input, 1f);
        rb.linearVelocity = direction * Hero.MoveSpeed;

        target = FindNearestEnemy();
        foreach (IAttack attack in attacks)
            attack.Tick();
    }

    private EnemyActor FindNearestEnemy()
    {
        // Pick randomly among the three closest enemies in range so
        // simultaneous attacks spread across the crowd instead of every
        // projectile chasing the same unlucky target.
        List<EnemyActor> candidates = new List<EnemyActor>();
        float targetRadius = 15f;

        // Target anything visible on screen regardless of device: the
        // radius comes from the camera frustum (aspect included), so wide
        // and narrow screens behave the same.
        if (gameplayCamera != null && gameplayCamera.orthographic)
        {
            float halfHeight = gameplayCamera.orthographicSize;
            float halfWidth = halfHeight * gameplayCamera.aspect;
            targetRadius = Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight) + 2f;
        }

        foreach (EnemyActor enemy in enemies.Items)
        {
            if (enemy == null || enemy.IsDead)
                continue;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);

            if (distance > targetRadius)
                continue;

            candidates.Add(enemy);
        }

        if (candidates.Count == 0)
            return null;

        candidates.Sort((a, b) =>
            Vector2.Distance(transform.position, a.transform.position)
                .CompareTo(Vector2.Distance(transform.position, b.transform.position)));

        int take = Mathf.Min(candidates.Count, 3);
        return candidates[Random.Range(0, take)];
    }

    private void HandleHeroDamaged(float amount)
    {

    }

    private void HandleHeroRestored(float amount)
    {

    }

    private void HandleTouch(Vector2 value)
    {
        touchDirection = value;
    }
}
