using TMPro;
using UnityEngine;

public class WaveNotifier : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float duration = 2f;

    private void Start()
    {
        Hide();
    }

    public void ShowWave(int waveNumber)
    {
        CancelInvoke(nameof(Hide));
        waveText.text = $"ВОЛНА {waveNumber}";
        canvasGroup.alpha = 1f;
        Invoke(nameof(Hide), duration);
    }

    private void Hide()
    {
        canvasGroup.alpha = 0f;
    }
}
