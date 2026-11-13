using System.Collections.Generic;
using System.Linq;

public class ResourceWallet
{
    private readonly List<ResourceState> states;
    private readonly Dictionary<string, ResourceState> resources;

    public ResourceWallet(List<ResourceState> states)
    {
        this.states = states;
        resources = states.ToDictionary(state => state.id);
    }

    public int Get(ResourceConfig resource)
    {
        return Get(resource.id);
    }

    public int Get(string id)
    {
        return resources.TryGetValue(id, out ResourceState state)
            ? state.amount
            : 0;
    }

    public void Add(ResourceConfig resource, int amount)
    {
        Add(resource.id, amount);
    }

    public void Add(string id, int amount)
    {
        if (!resources.TryGetValue(id, out ResourceState state))
        {
            state = new ResourceState { id = id };
            resources.Add(id, state);
            states.Add(state);
        }

        state.amount += amount;
    }

    public bool Spend(ResourceConfig resource, int amount)
    {
        return Spend(resource.id, amount);
    }

    public bool Spend(string id, int amount)
    {
        if (!resources.TryGetValue(id, out ResourceState state) ||
            state.amount < amount)
        {
            return false;
        }

        state.amount -= amount;
        return true;
    }
}
