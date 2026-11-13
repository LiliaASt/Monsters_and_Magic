using System.Collections.Generic;

public class Hero
{
    private readonly HeroConfig config;
    private readonly HeroState state;

    public string Id => state.id;
    public string DisplayName => config.displayName;
    public int GlobalLevel => state.globalLevel;
    public int CompletedLevels => state.completedLevels;
    public int CurrentLevel => state.currentLevel;

    public float MaxHealth => config.maxHealth;
    public float RegenPerSec => config.regenPerSec;
    public float MoveSpeed => config.moveSpeed;
    public IReadOnlyList<AttackDefinition> StartingAttacks => config.startingAttacks;

    public Hero(HeroConfig config, HeroState state)
    {
        this.config = config;
        this.state = state;
    }

    public void SetGlobalLevel(int level)
    {
        state.globalLevel = level;
    }

    public void SetProgress(int completedLevels, int currentLevel)
    {
        state.completedLevels = completedLevels;
        state.currentLevel = currentLevel;
    }

    public void CompleteLevel()
    {
        state.completedLevels++;
        state.currentLevel = state.completedLevels + 1;
        state.wins++;
    }

    public void RegisterDeath()
    {
        state.deaths++;
    }

    public void AddKills(int amount)
    {
        state.totalKills += amount;
    }
}
