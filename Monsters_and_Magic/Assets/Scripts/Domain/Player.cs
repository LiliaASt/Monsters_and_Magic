using System.Collections.Generic;
using System.Linq;

public class Player
{
    private readonly ProfileState state;
    private readonly Dictionary<string, Hero> heroes;

    public string Name => state.playerName;
    public ResourceWallet Resources { get; }
    public IReadOnlyCollection<Hero> Heroes => heroes.Values;
    public Hero SelectedHero => heroes[state.selectedHeroId];

    public Player(
        ProfileState state,
        IReadOnlyDictionary<string, HeroConfig> heroConfigs)
    {
        this.state = state;

        heroes = state.heroes.ToDictionary(
            heroState => heroState.id,
            heroState => new Hero(heroConfigs[heroState.id], heroState));

        Resources = new ResourceWallet(state.resources);
    }

    public void SetName(string value)
    {
        state.playerName = value;
    }

    public void SelectHero(string id)
    {
        state.selectedHeroId = id;
    }

    public bool ApplyRun(RunResult result, RunRewards rewards, string runId)
    {
        if (state.stats.appliedRunIds.Contains(runId))
            return false;

        state.stats.totalRuns++;
        state.stats.totalKills += result.Kills;

        if (result.State == RunState.Won)
        {
            state.stats.wins++;
            SelectedHero.CompleteLevel();
        }
        else if (result.State == RunState.Lost)
        {
            state.stats.deaths++;
            SelectedHero.RegisterDeath();
        }

        SelectedHero.AddKills(result.Kills);

        foreach (ResourceAmount reward in rewards.Items)
            Resources.Add(reward.resource, reward.amount);

        state.stats.appliedRunIds.Add(runId);
        state.sync.pending = true;
        state.sync.revision++;
        return true;
    }
}