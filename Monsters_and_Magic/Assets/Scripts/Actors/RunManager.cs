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
        run = new Run(config.killQuota);
        spawner.Spawned += HandleSpawned;
        hero.Hero.Health.Died += HandleHeroDeath;
    }

    private void HandleSpawned(EnemyActor enemy)
    {
        enemy.Died += HandleEnemyDeath;
    }

    private void HandleEnemyDeath(EnemyActor enemy)
    {
        run.Kill();

        if (run.QuotaReached)
            spawner.Stop();

        if (run.QuotaReached && spawner.AliveCount == 0)
            Finish(winScreen);
    }

    private void HandleHeroDeath()
    {
        run.Lose();
        Finish(deathScreen);
    }

    private void Finish(GameObject screen)
    {
        Time.timeScale = 0f;
        screen.SetActive(true);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
