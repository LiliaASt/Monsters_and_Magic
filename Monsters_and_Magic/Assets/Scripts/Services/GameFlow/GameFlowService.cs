using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowService
{
    private readonly ProfileService profile;
    private readonly string menuScene;
    private readonly string gameScene;

    private bool loading;

    public GameFlowService(
        ProfileService profile,
        string menuScene,
        string gameScene)
    {
        this.profile = profile;
        this.menuScene = menuScene;
        this.gameScene = gameScene;
    }

    public void StartGame()
    {
        Load(gameScene);
    }

    public void OpenMenu()
    {
        Load(menuScene);
    }

    public void Quit()
    {
        profile.Save();
        Application.Quit();
    }

    private void Load(string scene)
    {
        if (loading)
            return;

        loading = true;
        profile.Save();
        Time.timeScale = 1f;

        AsyncOperation operation = SceneManager.LoadSceneAsync(
            scene,
            LoadSceneMode.Single);

        operation.completed += _ => loading = false;
    }
}
