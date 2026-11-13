using System;

public class RunCompletionService
{
    private readonly Player player;
    private readonly ProfileService profile;

    public RunCompletionService(Player player, ProfileService profile)
    {
        this.player = player;
        this.profile = profile;
    }

    public RunRewards Complete(
        RunResult result,
        RunRewardProvider rewardProvider)
    {
        RunRewards rewards = rewardProvider.GetRewards(result);
        string runId = Guid.NewGuid().ToString("N");

        player.ApplyRun(result, rewards, runId);
        profile.Save();
        return rewards;
    }
}
