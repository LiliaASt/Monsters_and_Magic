using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public void Play()
    {
        GameContext.Instance.Flow.StartGame();
    }

    public void OnStartGameClicked()
    {
        Play();
    }

    public void Exit()
    {
        GameContext.Instance.Flow.Quit();
    }
}
