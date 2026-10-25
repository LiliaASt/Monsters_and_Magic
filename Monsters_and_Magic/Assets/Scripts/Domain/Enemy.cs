public class Enemy
{
    public Health Health { get; }
    public float MoveSpeed { get; }

    public Enemy(EnemyConfig config)
    {
        Health = new Health(config.maxHealth);
        MoveSpeed = config.moveSpeed;
    }
}
