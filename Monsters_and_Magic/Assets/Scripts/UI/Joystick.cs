using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Настройки джойстика")]
    public RectTransform visual;
    public RectTransform handle;
    public float handleRadius = 45f;
    public CanvasGroup canvasGroup;
    [SerializeField] private Vector2Channel move;

    private Vector2 inputVector;

    private void Awake()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = false;
        }

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;

        inputVector = Vector2.zero;
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
