using UnityEngine;

/// <summary>
/// Paper-2.5D depth cue: sprites lower on the gameplay plane are closer to
/// the camera and must draw in front. Assigns a dynamic sorting order from
/// the Y position so the hero can run behind and in front of tall props.
/// </summary>
public class YSort : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private int orderBias;

    private void Awake()
    {
        if (sprite == null)
        {
            // Prefer the Visual child; never the flat ground shadow -
            // otherwise the shadow gets the dynamic order and the real
            // sprite stays static.
            Transform visual = transform.Find("Visual");
            sprite = visual != null
                ? visual.GetComponent<SpriteRenderer>()
                : FindFirstNonShadowSprite();
        }
    }

    private SpriteRenderer FindFirstNonShadowSprite()
    {
        foreach (SpriteRenderer candidate in GetComponentsInChildren<SpriteRenderer>())
        {
            if (!candidate.name.Equals("ShadowBlob"))
                return candidate;
        }

        return null;
    }

    private void LateUpdate()
    {
        if (sprite == null)
            return;

        // Order is biased up so sprites never sink under the ground tilemaps
        // (which sit at -32000); the Y step of 10 keeps enough precision.
        int order = orderBias + 1000 - Mathf.RoundToInt(transform.position.y * 10f);
        sprite.sortingOrder = Mathf.Clamp(order, -30000, 30000);
    }
}
