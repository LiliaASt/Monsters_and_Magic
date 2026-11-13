using System.Collections.Generic;

public sealed class RunRewards
{
    public IReadOnlyList<ResourceAmount> Items { get; }

    public RunRewards(IReadOnlyList<ResourceAmount> items)
    {
        Items = items;
    }
}
