using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        spawner.Spawned += HandleSpawned;
        hero.Hero.Health.Died += HandleHeroDeath;

        deathScreen ??= BuildScreen("Вы погибли", "Принять смерть");
        winScreen ??= BuildScreen("Этап пройден", "Снова");
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
        Time.timeScale = 0f;
        screen.SetActive(true);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private GameObject BuildScreen(string title, string label)
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        GameObject screen = new GameObject(label + "Screen");
        screen.transform.SetParent(canvas.transform, false);

        RectTransform screenRect = screen.AddComponent<RectTransform>();
        screenRect.anchorMin = Vector2.zero;
        screenRect.anchorMax = Vector2.one;
        screenRect.offsetMin = Vector2.zero;
        screenRect.offsetMax = Vector2.zero;

        Image background = screen.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.85f);

        GameObject titleObject = new GameObject("Title");
        titleObject.transform.SetParent(screen.transform, false);
        Text titleText = titleObject.AddComponent<Text>();
        titleText.text = title;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 34;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.62f);
        titleRect.anchorMax = new Vector2(0.5f, 0.62f);
        titleRect.sizeDelta = new Vector2(700, 90);

        GameObject buttonObject = new GameObject("Restart");
        buttonObject.transform.SetParent(screen.transform, false);
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.22f, 0.22f, 0.22f);
        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(Restart);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.35f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.35f);
        buttonRect.sizeDelta = new Vector2(340, 90);

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);
        Text labelText = labelObject.AddComponent<Text>();
        labelText.text = label;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 24;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = Color.white;
        labelText.raycastTarget = false;
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        screen.SetActive(false);
        return screen;
    }
}
