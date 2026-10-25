using UnityEngine;

public interface IAttackSource
{
    Transform Origin { get; }
    Health TargetHealth { get; }
    float DistanceToTarget { get; }
    AttackStats Stats { get; }
}
