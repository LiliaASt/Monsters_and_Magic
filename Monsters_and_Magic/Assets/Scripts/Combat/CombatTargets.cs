using UnityEngine;

public static class CombatTargets
{
    public static IDamageable Get(Collider2D collider, CombatTeam attacker)
    {
        IDamageable target = collider.GetComponentInParent<IDamageable>();

        return target != null && target.Team != attacker && !target.Health.IsDead
            ? target
            : null;
    }
}
