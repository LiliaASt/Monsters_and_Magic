using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Настройки джойстика")]
    public RectTransform handle;
    public float handleRadius = 45f;
    public CanvasGroup canvasGroup;
    [SerializeField] private Vector2Channel move;

    private RectTransform visual;
    private Vector2 inputVector;

    private void Awake()
    {
        BuildVisual();
        StretchZone();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;

        inputVector = Vector2.zero;
    }

    private void BuildVisual()
    {
        Image zone = GetComponent<Image>();
        if (zone == null || zone.sprite == null)
            return;

        visual = new GameObject("Visual").AddComponent<RectTransform>();
        visual.SetParent(transform, false);
        visual.gameObject.layer = gameObject.layer;
        visual.anchorMin = new Vector2(0.5f, 0.5f);
        visual.anchorMax = new Vector2(0.5f, 0.5f);
        visual.pivot = new Vector2(0.5f, 0.5f);
        visual.sizeDelta = ((RectTransform)transform).sizeDelta;

        Image visualImage = visual.gameObject.AddComponent<Image>();
        visualImage.sprite = zone.sprite;
        visualImage.color = zone.color;
        visualImage.raycastTarget = false;

        if (handle != null)
            handle.SetParent(visual, false);
    }

    private void StretchZone()
    {
        RectTransform zone = (RectTransform)transform;
        zone.anchorMin = Vector2.zero;
        zone.anchorMax = Vector2.one;
        zone.offsetMin = Vector2.zero;
        zone.offsetMax = Vector2.zero;
        zone.pivot = new Vector2(0.5f, 0.5f);

        Image image = GetComponent<Image>();
        if (image != null)
        {
            image.sprite = null;
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (visual != null)
            visual.position = eventData.position;

        if (canvasGroup != null)
            canvasGroup.alpha = 0.6f;

        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (visual == null)
            return;

        Vector2 position = Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            visual,
            eventData.position,
            eventData.pressEventCamera,
            out position);

        position = Vector2.ClampMagnitude(position, handleRadius);

        if (handle != null)
            handle.anchoredPosition = position;

        inputVector = position / handleRadius;
        move?.Publish(inputVector);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;

        inputVector = Vector2.zero;
        move?.Publish(Vector2.zero);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void OnDisable()
    {
        if (handle != null) handle.anchoredPosition = Vector2.zero;
        inputVector = Vector2.zero;
        move?.Publish(Vector2.zero);
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }
}
