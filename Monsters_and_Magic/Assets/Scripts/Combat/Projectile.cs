using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Снаряд")]
    [SerializeField] private float lifeTime = 3f;

    private Rigidbody2D rb;
    private float damage;
    private float lifeTimer;
    private bool launched;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector3 targetPosition, float damage, float speed)
    {
        this.damage = damage;

        Vector2 direction = ((Vector2)targetPosition - rb.position).normalized;
        rb.linearVelocity = direction * speed;
        launched = true;
    }

    private void Update()
    {
        if (!launched)
            return;

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!launched)
            return;

        Health target = other.GetComponent<Health>();
        if (target == null)
            return;

        target.TakeDamage(damage);
        Destroy(gameObject);
    }
}
