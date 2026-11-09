using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class EndScreenController : MonoBehaviour
{
    [Header("UI Элементы")]
    public TextMeshProUGUI statsText;

    // Пока используем тестовые данные. Позже их будет передавать GameManager
    private void Start()
    {
        ShowResults("05:23", 42, 850, 1000);
    }

    public void ShowResults(string time, int kills, int xp, int maxXp)
    {
        if (statsText != null)
        {
            statsText.text = $"Время: {time}\nУбийства: {kills}\nОпыт: {xp} / {maxXp}";
        }
    }

    public void OnReturnToMenuClicked()
    {
        Debug.Log("Возврат в главное меню");
        // SceneManager.LoadScene("MainMenu"); // Раскомментируй, когда будешь готовы
    }
}