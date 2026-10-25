using UnityEngine;

public interface IAttackSource
{
    Transform Origin { get; }
    Transform TargetOrigin { get; }
    Health TargetHealth { get; }
    float DistanceToTarget { get; }
    AttackStats Stats { get; }
}
