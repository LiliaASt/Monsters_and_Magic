using UnityEngine;
using UnityEngine.SceneManagement;

public class RunManager : MonoBehaviour
{
    [SerializeField] private StageConfig config;
    [SerializeField] private HeroActor hero;
    [SerializeField] private EnemySpawner spawner;
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private GameObject winScreen;

    private Run run;

    private void Awake()
    {
        run = new Run(CreateGoal());
        run.Won += HandleWon;
        run.Lost += HandleLost;
        spawner.Spawned += HandleSpawned;
        hero.Hero.Health.Died += HandleHeroDeath;
    }

    private void Update()
    {
        run.Tick(Time.deltaTime);

        if (run.State != RunState.Active || !run.GoalReached)
            return;

        spawner.Stop();

        if (!run.RequiresClearing || spawner.AliveCount == 0)
            Finish(winScreen);
    }

    private StageGoal CreateGoal()
    {
        switch (config.goal)
        {
            case StageGoalType.SurviveTimer:
                return new SurviveGoal(config.surviveSeconds);
            default:
                return new KillGoal(config.killQuota);
        }
    }

    private void HandleWon()
    {
        Debug.Log("Win");
    }

    private void HandleLost()
    {
        Debug.Log("Lose");
    }

    private void HandleSpawned(EnemyActor enemy)
    {
        enemy.Died += HandleEnemyDeath;
    }

    private void HandleEnemyDeath(EnemyActor enemy)
    {
        run.Kill();
    }

    private void HandleHeroDeath()
    {
        run.Lose();
        Finish(deathScreen);
    }

    private void Finish(GameObject screen)
    {
        if (screen == null)
            return;

        Time.timeScale = 0f;
        screen.SetActive(true);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
