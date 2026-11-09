using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [Header("UI Элементы")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI killCounterText;
    public Slider xpSlider;

    [Header("Настройки")]
    public float xpPerKill = 10f; 
    public float maxXP = 100f; 

    private float playTime = 0f;
    private int kills = 0;
    private float currentXP = 0f;

    private void Update()
    {
        playTime += Time.deltaTime;
        UpdateTimer();
    }

    private void UpdateTimer()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(playTime / 60f);
        int seconds = Mathf.FloorToInt(playTime % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void OnEnemyKilled()
    {
        kills++;
        currentXP += xpPerKill;

            if (currentXP > maxXP)
            currentXP = maxXP;

        UpdateKillCounter();
        UpdateXPBar();

        Debug.Log($"[HUD] Убийство #{kills} | XP: {currentXP}/{maxXP}");
    }

    private void UpdateKillCounter()
    {
        if (killCounterText == null) return;
        killCounterText.text = $"💀 Kills: {kills}";
    }

    private void UpdateXPBar()
    {
        if (xpSlider == null) return;
        xpSlider.value = currentXP / maxXP; // От 0 до 1
    }

    // Для тестирования - добавь эту кнопку в инспекторе
    public void TestKill()
    {
        OnEnemyKilled();
    }
}