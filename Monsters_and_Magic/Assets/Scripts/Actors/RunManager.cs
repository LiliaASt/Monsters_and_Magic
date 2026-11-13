using System;
using UnityEngine;

public class RunManager : MonoBehaviour
{
    [SerializeField] private StageConfig config;
    [SerializeField] private HeroActor hero;
    [SerializeField] private EnemySpawner spawner;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private RunRewardProvider rewardProvider;
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private GameObject winScreen;

    private Run run;

    public Run CurrentRun => run;
    public RunCompletion Completion { get; private set; }

    public event Action<RunCompletion> Finished;

    private void Start()
    {
        Hero selectedHero = GameContext.Instance.Player.SelectedHero;
        run = new Run(CreateGoal(), selectedHero.StartingAttacks,
            config.levelBaseXp, config.levelGrowth);
        hero.Initialize(selectedHero, run);

        run.Won += HandleWon;
        run.Lost += HandleLost;

        spawner.Spawned += HandleSpawned;
        waveManager.Completed += HandleWavesCompleted;
        hero.Health.Died += HandleHeroDeath;
        hero.XpCollected += run.AddXp;
        // Presentation wiring stays here (composition root): the hero actor
        // itself knows nothing about level-up effects.
        LevelUpPresenter levelUpPresenter = hero.GetComponent<LevelUpPresenter>();
        levelUpPresenter?.Bind(run);

        WaveRules rules = config.location != null ? config.location.waves : config.waveRules;
        waveManager.Initialize(rules, config.waveCount);
    }

    private void Update()
    {
        run.Tick(Time.deltaTime);

        if (run.State != RunState.Active)
            return;

        if (config.goal == StageGoalType.CompleteWaves)
            return;

        if (!run.GoalReached)
            return;

        waveManager.Stop();

        if (!run.RequiresClearing || spawner.AliveCount == 0)
            run.Win();
    }

    private StageGoal CreateGoal()
    {
        switch (config.goal)
        {
            case StageGoalType.SurviveTimer:
                return new SurviveGoal(config.surviveSeconds);
            case StageGoalType.CompleteWaves:
                return new WaveGoal();
            default:
                return new KillGoal(config.killQuota);
        }
    }

    private void HandleWavesCompleted()
    {
        if (config.goal == StageGoalType.CompleteWaves)
            run.Win();
    }

    private void HandleWon()
    {
        Finish(winScreen);
    }

    private void HandleLost()
    {
        Finish(deathScreen);
    }

    private void HandleSpawned(EnemyActor enemy)
    {
        enemy.Died += HandleEnemyDeath;
    }

    private void HandleEnemyDeath(EnemyActor enemy)
    {
        enemy.Died -= HandleEnemyDeath;
        run.Kill();
    }

    private void HandleHeroDeath()
    {
        run.Lose();
    }


    private void Finish(GameObject screen)
    {
        waveManager.Stop();
        spawner.Stop();

        if (GameContext.Instance != null)
            GameContext.Instance.MarkProfileDirty();

        RunResult result = run.CreateResult();
        RunRewards rewards = GameContext.Instance.RunCompletion.Complete(result, rewardProvider);

        Completion = new RunCompletion(result, rewards);
        Finished?.Invoke(Completion);

        Time.timeScale = 0f;
        screen.SetActive(true);
    }
}
