using System.Linq;
using UnityEngine;

public class GameContext : MonoBehaviour
{
    [SerializeField] private HeroConfig[] heroConfigs;
    [SerializeField] private HeroConfig[] startingHeroes;
    [SerializeField] private string menuScene = "MainMenu";
    [SerializeField] private string gameScene = "Game";

    public static GameContext Instance { get; private set; }

    public ProfileService Profile { get; private set; }
    public GameFlowService Flow { get; private set; }
    public RunCompletionService RunCompletion { get; private set; }
    public Player Player => Profile.Player;
    private ConnectivityService connectivity;
    private CloudSyncService cloudSync;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        var configs = heroConfigs.ToDictionary(config => config.id);
        var storage = new JsonFileStorage<ProfileState>("profile.json");
        var factory = new ProfileFactory(startingHeroes);

        Profile = new ProfileService(storage, factory, configs);
        Profile.Load();

        Flow = new GameFlowService(
            Profile,
            menuScene,
            gameScene);

        RunCompletion = new RunCompletionService(
            Player,
            Profile);

        connectivity = gameObject.AddComponent<ConnectivityService>();
        cloudSync = gameObject.AddComponent<CloudSyncService>();
        cloudSync.Initialize(Profile, connectivity);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            Profile.Save();
    }

    private void OnApplicationQuit()
    {
        Profile.Save();
    }

    public void Save()
    {
        Profile.Save();
        cloudSync.MarkDirty();
    }

    public void MarkProfileDirty()
    {
        cloudSync.MarkDirty();
    }
}
