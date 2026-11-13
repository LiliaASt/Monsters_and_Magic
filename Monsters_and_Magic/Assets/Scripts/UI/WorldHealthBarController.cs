using UnityEngine;

public class WorldHealthBarController : MonoBehaviour
{
    [SerializeField] private WorldHealthBarView view;
    [SerializeField] private float verticalGap = 0.15f;
    [SerializeField] private float visibleDuration = 1.25f;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float fillSpeed = 4f;

    private Health health;
    private SpriteRenderer ownerRenderer;
    private float displayedValue;
    private float hideAt;
    private float alpha;

    public void Bind(Health value, SpriteRenderer renderer)
    {
        health = value;
        ownerRenderer = renderer;
        displayedValue = health.Current / health.Max;

        view.SetValue(displayedValue);
        view.SetAlpha(0f);

        health.Damaged += HandleChanged;
        health.Restored += HandleChanged;
    }

    private void Update()
    {
        float target = health.Current / health.Max;

        displayedValue = Mathf.MoveTowards(
            displayedValue,
            target,
            fillSpeed * Time.unscaledDeltaTime);

        view.SetValue(displayedValue);

        float targetAlpha = Time.unscaledTime < hideAt ? 1f : 0f;
        float fadeSpeed = fadeDuration > 0f ? 1f / fadeDuration : 1000f;

        alpha = Mathf.MoveTowards(
            alpha,
            targetAlpha,
            fadeSpeed * Time.unscaledDeltaTime);

        view.SetAlpha(alpha);
    }

    private void LateUpdate()
    {
        if (ownerRenderer == null)
            return;

        Bounds bounds = ownerRenderer.bounds;
        transform.position = new Vector3(
            bounds.center.x,
            bounds.max.y + verticalGap,
            ownerRenderer.transform.position.z);

        transform.rotation = Quaternion.identity;
    }

    private void OnDestroy()
    {
        health.Damaged -= HandleChanged;
        health.Restored -= HandleChanged;
    }

    private void HandleChanged(float amount)
    {
        alpha = 1f;
        hideAt = Time.unscaledTime + visibleDuration;
        view.SetAlpha(alpha);
    }
}
