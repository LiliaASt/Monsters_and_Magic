using System;
using System.Collections.Generic;

[Serializable]
public class ProfileState
{
    public const int CurrentVersion = 3;

    public int version = CurrentVersion;
    public string playerName = "";
    public string selectedHeroId;
    public List<ResourceState> resources = new();
    public List<HeroState> heroes = new();
    public PlayerStatsState stats = new();
    public SyncState sync = new();

    public void Migrate()
    {
        if (version < 3)
        {
            stats = new PlayerStatsState();
            sync = new SyncState();
            version = 3;
        }
    }
}

[Serializable]
public class PlayerStatsState
{
    public int totalRuns;
    public int wins;
    public int deaths;
    public int totalKills;
    public List<string> appliedRunIds = new();
}

[Serializable]
public class SyncState
{
    public long revision;
    public bool pending;
    public string lastSyncedHash;
}
