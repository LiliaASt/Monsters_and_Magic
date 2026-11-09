using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WaveNotifier : MonoBehaviour
{
    [Header("UI Элементы")]
    public TextMeshProUGUI waveText;
    public CanvasGroup canvasGroup;

    private void Start()
    {
        HideNotification();
    }

    public void ShowWave(int waveNumber)
    {
        if (waveText != null)
            waveText.text = $"ВОЛНА {waveNumber}";

        if (canvasGroup != null)
            canvasGroup.alpha = 1f; 

        Invoke(nameof(HideNotification), 2f);
    }

    private void HideNotification()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f; 
    }

    public void TestWave()
    {
        ShowWave(1);
    }
}