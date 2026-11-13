public sealed class RunCompletion
{
    public RunResult Result { get; }
    public RunRewards Rewards { get; }

    public RunCompletion(RunResult result, RunRewards rewards)
    {
        Result = result;
        Rewards = rewards;
    }
}
