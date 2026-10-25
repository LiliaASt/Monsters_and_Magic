using System;

public enum RunState
{
    Active,
    Won,
    Lost
}

public class Run
{
    private readonly int quota;

    public RunState State { get; private set; }
    public int Kills { get; private set; }
    public bool QuotaReached => Kills >= quota;

    public event Action Won;
    public event Action Lost;

    public Run(int quota)
    {
        this.quota = quota;
        State = RunState.Active;
    }

    public void Kill()
    {
        if (State != RunState.Active)
            return;

        Kills++;
    }

    public void Win()
    {
        if (State != RunState.Active)
            return;

        State = RunState.Won;
        Won?.Invoke();
    }

    public void Lose()
    {
        if (State != RunState.Active)
            return;

        State = RunState.Lost;
        Lost?.Invoke();
    }
}
