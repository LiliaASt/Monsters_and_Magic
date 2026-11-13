using UnityEngine;

/// <summary>
/// Experience orb dropped by enemies. One entity, several tiers: the color
/// encodes the XP value. Orbs scatter with a pseudo-height bounce (visual
/// child rises and falls with fake gravity while the ground position stays
/// on the gameplay plane) and magnet to the hero when he comes close.
/// </summary>
public class XPOrb : MonoBehaviour
{
    [SerializeField] private Transform visual;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private float magnetRadius = 6.5f;
    [SerializeField] private float magnetStrength = 26f;
    [Tooltip("Inside this radius the attraction stops - the orb waits to be picked up.")]
    [SerializeField] private float deadZoneRadius = 0.35f;
    [SerializeField] private float maxSpeed = 12f;

    private int value;
    private float height;
    private float verticalVelocity;
    private HeroActor hero;
    private Rigidbody2D body;

    public void Initialize(int xp)
    {
        value = xp;

        GameCatalog.OrbSettings settings = GameCatalog.Load()?.orbs;
        Color color = new Color(0.55f, 2.4f, 3.2f, 1f);
        float scale = 0.42f;

        if (settings != null)
        {
            if (xp >= settings.largeXp)
            {
                color = settings.largeColor;
                scale = settings.largeScale;
            }
            else if (xp >= settings.mediumXp)
            {
                color = settings.mediumColor;
                scale = settings.mediumScale;
            }
            else
            {
                color = settings.smallColor;
                scale = settings.smallScale;
            }
        }

        if (sprite != null)
        {
            sprite.color = color;
            visual.localScale = Vector3.one * scale;
        }

    }

    /// <summary>Throws the orb out of a dying enemy.</summary>
    public void Scatter(Vector2 direction, float force)
    {
        body.AddForce(direction * force, ForceMode2D.Impulse);
        height = 0.9f;
        verticalVelocity = 3.2f;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        hero = HeroActor.Instance;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // Fake gravity on the visual height: the orb pops up, falls, bounces
        // and settles while the ground position stays on the gameplay plane.
        verticalVelocity -= 14f * dt;
        height += verticalVelocity * dt;

        if (height <= 0f)
        {
            height = 0f;
            verticalVelocity = -verticalVelocity * 0.45f;

            if (Mathf.Abs(verticalVelocity) < 0.9f)
                verticalVelocity = 0f;
        }

        if (visual != null)
            visual.localPosition = new Vector3(0f, height, 0f);
    }

    private void FixedUpdate()
    {
        if (hero == null)
        {
            hero = HeroActor.Instance;
            return;
        }

        Vector2 toHero = (Vector2)hero.transform.position - body.position;
        float distance = toHero.magnitude;

        if (distance <= magnetRadius && distance > deadZoneRadius)
        {
            // Real physics magnet: force falls off with the square of the
            // distance, the rigidbody integrates it.
            float falloff = magnetRadius * magnetRadius
                / Mathf.Max(distance * distance, 1f);
            body.AddForce(toHero.normalized * (magnetStrength * falloff));
        }

        if (body.linearVelocity.sqrMagnitude > maxSpeed * maxSpeed)
            body.linearVelocity = body.linearVelocity.normalized * maxSpeed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<HeroActor>() != null)
            Collect();
    }

    private void Collect()
    {
        hero.GainXP(value);
        Destroy(gameObject);
    }
}
