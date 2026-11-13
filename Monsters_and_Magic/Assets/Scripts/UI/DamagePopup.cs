using UnityEngine;

/// <summary>
/// Animated damage number: pops up with a small punch, floats upward and
/// fades out. Plain TextMesh in world space - no canvas dependencies.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    private float age;
    private TextMesh label;
    private Vector3 baseScale;

    public static DamagePopup Spawn(Vector3 position, float amount)
    {
        GameObject popup = new GameObject("DamagePopup");
        popup.transform.position = position + new Vector3(
            Random.Range(-0.25f, 0.25f), 0.5f, 0f);

        DamagePopup component = popup.AddComponent<DamagePopup>();
        component.label = popup.AddComponent<TextMesh>();
        component.label.text = Mathf.RoundToInt(amount).ToString();
        component.label.fontSize = 64;
        component.label.characterSize = 0.052f;
        component.label.anchor = TextAnchor.MiddleCenter;
        component.label.richText = false;

        component.label.color = amount >= 20f
            ? new Color(2.2f, 1.4f, 0.4f, 1f)
            : new Color(2.0f, 2.0f, 2.4f, 1f);

        MeshRenderer renderer = popup.GetComponent<MeshRenderer>();
        renderer.sortingLayerName = "VFX";
        renderer.sortingOrder = 900;

        // Face the tilted camera so the digits stand upright on screen.
        Camera camera = Camera.main;

        if (camera != null)
            popup.transform.rotation = camera.transform.rotation;

        component.baseScale = Vector3.one;
        return component;
    }

    private void Awake()
    {
        label = GetComponent<TextMesh>();
        baseScale = transform.localScale;
    }

    private void Update()
    {
        age += Time.deltaTime;

        transform.position += Vector3.up * (1.6f * Time.deltaTime);

        float punch = age < 0.12f
            ? Mathf.Lerp(1.6f, 1f, age / 0.12f)
            : 1f;
        transform.localScale = baseScale * punch;

        Color color = label.color;
        color.a = Mathf.Clamp01(1f - (age - 0.45f) / 0.4f);
        label.color = color;

        if (age >= 0.85f)
            Destroy(gameObject);
    }
}
