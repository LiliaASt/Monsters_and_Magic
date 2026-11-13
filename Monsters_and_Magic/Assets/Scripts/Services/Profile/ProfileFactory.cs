using System.Collections.Generic;
using System.Linq;

public class ProfileFactory
{
    private readonly IReadOnlyList<HeroConfig> startingHeroes;

    public ProfileFactory(IEnumerable<HeroConfig> startingHeroes)
    {
        this.startingHeroes = startingHeroes.ToArray();
    }

    public ProfileState Create()
    {
        List<HeroState> heroes = startingHeroes
            .Select(config => new HeroState
            {
                id = config.id,
                globalLevel = 1,
                completedLevels = 0,
                currentLevel = 1
            })
            .ToList();

        return new ProfileState
        {
            version = ProfileState.CurrentVersion,
            selectedHeroId = heroes[0].id,
            resources = new List<ResourceState>(),
            heroes = heroes
        };
    }
}
