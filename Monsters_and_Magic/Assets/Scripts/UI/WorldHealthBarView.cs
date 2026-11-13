using UnityEngine;
using UnityEngine.UI;

public class WorldHealthBarView : MonoBehaviour
{
    [SerializeField] private Image fill;
    [SerializeField] private CanvasGroup canvasGroup;

    public void SetValue(float value)
    {
        fill.fillAmount = Mathf.Clamp01(value);
    }

    public void SetAlpha(float value)
    {
        canvasGroup.alpha = value;
    }
}
