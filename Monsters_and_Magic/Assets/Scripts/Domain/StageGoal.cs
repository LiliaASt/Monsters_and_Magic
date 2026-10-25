public abstract class StageGoal
{
    public abstract bool Reached(float elapsed, int kills);
    public virtual bool RequiresClearing => false;
}

public class KillGoal : StageGoal
{
    private readonly int quota;

    public KillGoal(int quota)
    {
        this.quota = quota;
    }

    public override bool Reached(float elapsed, int kills)
    {
        return kills >= quota;
    }

    public override bool RequiresClearing => true;
}

public class SurviveGoal : StageGoal
{
    private readonly float duration;

    public SurviveGoal(float duration)
    {
        this.duration = duration;
    }

    public override bool Reached(float elapsed, int kills)
    {
        return elapsed >= duration;
    }
}
