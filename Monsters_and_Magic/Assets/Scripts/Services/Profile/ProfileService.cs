using UnityEngine;

using System.Collections.Generic;

public class ProfileService
{
    private readonly ILocalStorage<ProfileState> storage;
    private readonly ProfileFactory factory;
    private readonly IReadOnlyDictionary<string, HeroConfig> heroConfigs;

    private ProfileState state;

    public Player Player { get; private set; }

    public ProfileService(
        ILocalStorage<ProfileState> storage,
        ProfileFactory factory,
        IReadOnlyDictionary<string, HeroConfig> heroConfigs)
    {
        this.storage = storage;
        this.factory = factory;
        this.heroConfigs = heroConfigs;
    }

    public void Load()
    {
        bool exists = storage.Exists;

        state = exists
            ? storage.Load()
            : factory.Create();

        state.Migrate();
        Player = new Player(state, heroConfigs);

        if (!exists)
            Save();
    }

    public void Save()
    {
        storage.Save(state);
    }

    public long CurrentRevision => state.sync.revision;

    public bool HasPendingChanges => state.sync.pending;

    public string Serialize()
    {
        return JsonUtility.ToJson(state);
    }

    public void MarkSynced()
    {
        state.sync.pending = false;
        Save();
    }

    public void ReplaceState(ProfileState replacement)
    {
        state = replacement;
        Player = new Player(state, heroConfigs);
        Save();
    }
}
