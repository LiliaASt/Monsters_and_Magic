using System;
using System.Collections.Generic;

public enum RunState
{
    Active,
    Won,
    Lost
}

public class Run
{
    private readonly StageGoal goal;
    private readonly Dictionary<AttackDefinition, AttackInstance> attacks = new();

    public RunState State { get; private set; }
    public int Kills { get; private set; }
    public int Xp { get; private set; }
    public int Level { get; private set; } = 1;
    public int XpToNext { get; private set; }
    public float Elapsed { get; private set; }
    public bool GoalReached => goal.Reached(Elapsed, Kills);
    public bool RequiresClearing => goal.RequiresClearing;
    public IReadOnlyCollection<AttackInstance> Attacks => attacks.Values;

    public event Action Won;
    public event Action Lost;
    public event Action<int> LevelUp;
    public event Action<AttackInstance> AttackAdded;
    public event Action<AttackInstance> AttackUpgraded;

    private readonly float levelBaseXp;
    private readonly float levelGrowth;

    public Run(StageGoal goal, IEnumerable<AttackDefinition> startingAttacks,
        float levelBaseXp = 12f, float levelGrowth = 1.35f)
    {
        this.goal = goal;
        this.levelBaseXp = Math.Max(1f, levelBaseXp);
        this.levelGrowth = Math.Max(1f, levelGrowth);
        XpToNext = (int)Math.Ceiling(this.levelBaseXp);
        State = RunState.Active;

        foreach (AttackDefinition definition in startingAttacks)
            AddAttack(definition);
    }

    public bool AddAttack(AttackDefinition definition)
    {
        if (State != RunState.Active || attacks.ContainsKey(definition))
            return false;

        var instance = new AttackInstance(definition);
        attacks.Add(definition, instance);
        AttackAdded?.Invoke(instance);
        return true;
    }

    public bool UpgradeAttack(AttackDefinition definition)
    {
        if (State != RunState.Active || !attacks.TryGetValue(definition, out AttackInstance instance))
            return false;

        if (!instance.TryUpgrade())
            return false;

        AttackUpgraded?.Invoke(instance);
        return true;
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

    public void AddXp(int amount)
    {
        if (State != RunState.Active)
            return;

        Xp += amount;

        // Overflow carries into the next (rescaled) threshold: the bar
        // restarts from the remainder, never losing progress.
        while (Xp >= XpToNext && XpToNext > 0)
        {
            Xp -= XpToNext;
            Level++;
            XpToNext = (int)Math.Ceiling(
                levelBaseXp * Math.Pow(levelGrowth, Level - 1));
            LevelUp?.Invoke(Level);
        }
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

    public RunResult CreateResult()
    {
        return new RunResult(State, Elapsed, Kills, Xp);
    }
}
