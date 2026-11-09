using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ProfileController : MonoBehaviour
{
    [Header("UI Элементы")]
    public TMP_InputField nameInput;

    private const string PROFILE_KEY = "PlayerProfile_Name";

    private void Start()
    {
        string savedName = PlayerPrefs.GetString(PROFILE_KEY, "");

        if (!string.IsNullOrEmpty(savedName))
        {
            Debug.Log($"[Profile] Загружен профиль: {savedName}");
            LoadGame();
        }
        else
        {
            Debug.Log("[Profile] Создание нового профиля");
        }
    }

    public void OnContinueClicked()
    {
        string playerName = nameInput.text.Trim();

        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("[Profile] Имя не может быть пустым!");
            return;
        }

        PlayerPrefs.SetString(PROFILE_KEY, playerName);
        PlayerPrefs.Save();

        Debug.Log($"[Profile] Профиль создан: {playerName}");

        LoadGame();
    }

    private void LoadGame()
    {
        Debug.Log("Загрузка главного меню...");
        // SceneManager.LoadScene("MainMenu"); // позже
    }
}