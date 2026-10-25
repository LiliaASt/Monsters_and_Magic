using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Настройки джойстика")]
    public RectTransform handle;
    public float handleRadius = 45f;
    public CanvasGroup canvasGroup;

    private Vector2 inputVector;

    public Vector2 Direction { get => inputVector; }

    private void Awake()
    {
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

    public void OnPointerDown(PointerEventData eventData)
    {
        this.transform.position = eventData.position;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.6f;
        }

        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position = Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)this.transform,
            eventData.position,
            eventData.pressEventCamera,
            out position);

        position = Vector2.ClampMagnitude(position, handleRadius);

        if (handle != null)
            handle.anchoredPosition = position;

        inputVector = position / handleRadius;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;

        inputVector = Vector2.zero;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void OnDisable()
    {
        if (handle != null) handle.anchoredPosition = Vector2.zero;
        inputVector = Vector2.zero;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }
}