using System;

public class Health
{
    private float max;
    private float regen;

    public float Max => max;
    public float Current { get; private set; }
    public float Regen => regen;
    public bool IsDead => Current <= 0f;

    public event Action<float> Damaged;
    public event Action<float> Restored;
    public event Action Died;

    public Health(float max, float regen = 0f)
    {
        this.max = MathF.Max(1f, max);
        this.regen = MathF.Max(0f, regen);
        Current = this.max;
    }

    public void Tick(float deltaTime)
    {
        if (IsDead || regen <= 0f)
            return;

        Restore(regen * deltaTime);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        Current = MathF.Max(0f, Current - amount);
        Damaged?.Invoke(amount);

        if (Current <= 0f)
            Died?.Invoke();
    }

    public void Restore(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        float before = Current;
        Current = MathF.Min(max, Current + amount);

        float restored = Current - before;
        if (restored > 0f)
            Restored?.Invoke(restored);
    }

    public void SetMax(float value, bool refill = true)
    {
        max = MathF.Max(1f, value);
        Current = refill ? max : MathF.Min(Current, max);
    }

    public void AddMax(float bonus, bool restoreBonus = false)
    {
        SetMax(max + bonus);
        if (restoreBonus)
            Restore(bonus);
    }

    public void SetRegen(float value)
    {
        regen = MathF.Max(0f, value);
    }
}
