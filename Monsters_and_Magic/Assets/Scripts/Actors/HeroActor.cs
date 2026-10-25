using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody2D))]
public class HeroActor : MonoBehaviour, IAttackSource
{
    [SerializeField] private HeroConfig config;
    [SerializeField] private Vector2Channel move;
    [SerializeField] private EnemySet enemies;
    [SerializeField] private float turnSpeed = 720f;

    public Hero Hero { get; private set; }
    public bool IsDead => Hero.Health.IsDead;
    public Transform Origin => transform;
    public Health TargetHealth { get; private set; }
    public AttackStats Stats => config.attack;
    public float DistanceToTarget => TargetHealth == null
        ? float.MaxValue
        : Vector2.Distance(Origin.position, TargetHealth.transform.position);

    private Rigidbody2D rb;
    private InputAction moveAction;
    private IAttack attack;
    private Vector2 touchDirection;

    private void Awake()
    {
        Hero = new Hero(config);
        rb = GetComponent<Rigidbody2D>();
        moveAction = GetComponent<PlayerInput>().actions["Move"];
        attack = AttackFactory.Create(config.attack);
        attack.Initialize(this);
    }

    private void OnEnable()
    {
        move.Subscribe(HandleTouch);
    }

    private void OnDisable()
    {
        move.Unsubscribe(HandleTouch);
    }

    private void Update()
    {
        Hero.Tick(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        Vector2 keyboard = moveAction.ReadValue<Vector2>();
        Vector2 direction = touchDirection.sqrMagnitude > 0f ? touchDirection : keyboard;

        Hero.SetDirection(direction);
        rb.linearVelocity = Hero.MoveVelocity;
        Turn();

        TargetHealth = FindNearestEnemy()?.Health;
        attack.Tick();
    }

    private void Turn()
    {
        if (Hero.Direction.sqrMagnitude <= 0f)
            return;

        float targetAngle = Mathf.Atan2(Hero.Direction.y, Hero.Direction.x) * Mathf.Rad2Deg;
        rb.MoveRotation(Mathf.MoveTowardsAngle(rb.rotation, targetAngle, turnSpeed * Time.fixedDeltaTime));
    }

    private EnemyActor FindNearestEnemy()
    {
        EnemyActor nearest = null;
        float best = float.MaxValue;

        foreach (EnemyActor enemy in enemies.Items)
        {
            if (enemy == null || enemy.IsDead)
                continue;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < best)
            {
                best = distance;
                nearest = enemy;
            }
        }

        return nearest;
    }

    private void HandleTouch(Vector2 direction)
    {
        touchDirection = direction;
    }
}
