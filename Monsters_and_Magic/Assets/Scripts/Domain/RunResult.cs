public sealed class RunResult
{
    public RunState State { get; }
    public float Elapsed { get; }
    public int Kills { get; }
    public int Xp { get; }

    public RunResult(
        RunState state,
        float elapsed,
        int kills,
        int xp)
    {
        State = state;
        Elapsed = elapsed;
        Kills = kills;
        Xp = xp;
    }
}
