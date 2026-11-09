using UnityEngine;
using UnityEngine.SceneManagement; 

public class MainMenuController : MonoBehaviour
{
    public void OnStartGameClicked()
    {
        Debug.Log("Кнопка 'Начать забег' нажата! Загружаем игру...");

        // ПОКА ЗАКОММЕНТИРОВАНО, так как сцены игры ещё нет:
        // SceneManager.LoadScene("SampleScene"); 
    }

    public void OnQuitGameClicked()
    {
        Debug.Log("👋 Выход из игры");
        // Application.Quit(); // Работает только в собранной игре (APK/EXE), не в редакторе
    }
}