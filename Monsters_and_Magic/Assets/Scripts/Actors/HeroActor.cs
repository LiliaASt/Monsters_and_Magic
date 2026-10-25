using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody2D))]
public class HeroActor : MonoBehaviour
{
    [SerializeField] private HeroConfig config;
    [SerializeField] private Vector2Channel move;

    public Hero Hero { get; private set; }
    public bool IsDead => Hero.Health.IsDead;

    private Rigidbody2D rb;
    private InputAction moveAction;
    private Vector2 touchDirection;

    private void Awake()
    {
        Hero = new Hero(config);
        rb = GetComponent<Rigidbody2D>();
        moveAction = GetComponent<PlayerInput>().actions["Move"];
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
    }

    private void HandleTouch(Vector2 direction)
    {
        touchDirection = direction;
    }
}
