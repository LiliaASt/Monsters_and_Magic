using UnityEngine;

public class Hero
{
    private Vector2 direction;

    public Health Health { get; }
    public float MoveSpeed { get; private set; }
    public Vector2 Direction => direction;
    public Vector2 MoveVelocity => direction * MoveSpeed;

    public Hero(HeroConfig config)
    {
        Health = new Health(config.maxHealth, config.regenPerSec);
        MoveSpeed = config.moveSpeed;
    }

    public void SetDirection(Vector2 value)
    {
        direction = Vector2.ClampMagnitude(value, 1f);
    }

    public void Tick(float deltaTime)
    {
        Health.Tick(deltaTime);
    }
}
