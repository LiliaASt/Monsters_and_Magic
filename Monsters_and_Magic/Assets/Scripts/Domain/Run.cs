using System;

public enum RunState
{
    Active,
    Won,
    Lost
}

public class Run
{
    private readonly StageGoal goal;

    public RunState State { get; private set; }
    public int Kills { get; private set; }
    public float Elapsed { get; private set; }
    public bool GoalReached => goal.Reached(Elapsed, Kills);
    public bool RequiresClearing => goal.RequiresClearing;

    public event Action Won;
    public event Action Lost;

    public Run(StageGoal goal)
    {
        this.goal = goal;
        State = RunState.Active;
    }

    public void Tick(float deltaTime)
    {
        if (State != RunState.Active)
            return;

        Elapsed += deltaTime;
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
