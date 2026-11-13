using UnityEngine;

/// <summary>
/// Presentation-only component for paper-2.5D characters: moves the sprite
/// into a Visual child (billboard target), adds flip/sort behaviour and a
/// catalog-driven blob shadow. Gameplay actors stay clean of any of this.
/// </summary>
public class PaperCharacterPresentation : MonoBehaviour
{
    private void Awake()
    {
        if (GetComponent<Billboard>() == null)
            gameObject.AddComponent<Billboard>();

        if (GetComponent<SpriteFlipper>() == null)
            gameObject.AddComponent<SpriteFlipper>();

        if (GetComponent<YSort>() == null)
            gameObject.AddComponent<YSort>();

        // Move the root sprite into a Visual child so the billboard rotation
        // leaves the ground shadow flat.
        SpriteRenderer rootSprite = GetComponent<SpriteRenderer>();

        if (rootSprite != null && rootSprite.sprite != null
            && transform.Find("Visual") == null)
        {
            GameObject visualObject = new GameObject("Visual");
            visualObject.transform.SetParent(transform, false);
            SpriteRenderer visual = visualObject.AddComponent<SpriteRenderer>();
            visual.sprite = rootSprite.sprite;
            visual.color = rootSprite.color;
            visual.sortingOrder = rootSprite.sortingOrder;
            Destroy(rootSprite);

            GetComponent<Billboard>().SetVisual(visualObject.transform);
            GetComponent<SpriteFlipper>().SetModel(visualObject.transform);
        }

        GameCatalog catalog = GameCatalog.Load();

        if (catalog == null
            || catalog.presentation.shadowSprite == null
            || transform.Find("ShadowBlob") != null)
            return;

        SpriteRenderer ownSprite = GetComponentInChildren<SpriteRenderer>();

        if (ownSprite == null || ownSprite.sprite == null)
            return;

        GameObject shadow = new GameObject("ShadowBlob");
        shadow.transform.SetParent(transform, false);
        SpriteRenderer shadowRenderer = shadow.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = catalog.presentation.shadowSprite;
        shadowRenderer.sortingOrder = -31000;

        float width = ownSprite.sprite.bounds.size.x;
        float scale = Mathf.Clamp(
            width * catalog.presentation.shadowScale, 0.3f, 1.15f);
        shadow.transform.localScale = new Vector3(scale, scale, 1f);

        float bottom = ownSprite.sprite.bounds.center.y
            - ownSprite.sprite.bounds.extents.y;
        shadow.transform.localPosition = new Vector3(0f, bottom + 0.05f, 0f);
    }
}
