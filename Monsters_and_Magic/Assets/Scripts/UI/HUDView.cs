using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI killCounterText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Slider xpSlider;

    public void SetTime(float elapsed)
    {
        int minutes = Mathf.FloorToInt(elapsed / 60f);
        int seconds = Mathf.FloorToInt(elapsed % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void SetStats(int kills)
    {
        killCounterText.text = $"Kills: {kills}";
    }

    public void SetLevel(int level)
    {
        if (levelText != null)
            levelText.text = $"Lv {level}";
    }

    public void SetXpProgress(float ratio)
    {
        if (xpSlider != null)
            xpSlider.value = Mathf.Clamp01(ratio);
    }

    public void SetXpBarVisible(bool visible)
    {
        xpSlider.gameObject.SetActive(visible);
    }
}
