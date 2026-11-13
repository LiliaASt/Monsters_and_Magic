using System;

[Serializable]
public class AttackStats
{
    public float damage = 5f;
    public float range = 2f;
    public float cooldown = 1f;

    public AttackStats Copy()
    {
        return new AttackStats
        {
            damage = damage,
            range = range,
            cooldown = cooldown
        };
    }
}
