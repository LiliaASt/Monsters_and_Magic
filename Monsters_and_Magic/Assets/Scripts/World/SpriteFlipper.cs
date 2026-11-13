using UnityEngine;

/// <summary>
/// Flips a sprite left/right with movement (Survivor.io-style): no rotation,
/// just a horizontal mirror around the vertical billboard axis.
/// </summary>
public class SpriteFlipper : MonoBehaviour
{
    [SerializeField] private Transform model;

    public void SetModel(Transform value)
    {
        model = value;
    }

    private Rigidbody2D body;
    private Vector3 baseScale;

    private void Awake()
    {
        body = GetComponentInParent<Rigidbody2D>();

        if (model == null)
            model = transform;

        baseScale = model.localScale;
    }

    private void Update()
    {
        if (body == null)
            return;

        float vx = body.linearVelocity.x;

        if (Mathf.Abs(vx) < 0.15f)
            return;

        Vector3 scale = baseScale;
        scale.x = Mathf.Abs(baseScale.x) * (vx > 0f ? 1f : -1f);
        model.localScale = scale;
    }
}
