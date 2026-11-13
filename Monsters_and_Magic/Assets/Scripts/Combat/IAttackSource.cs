using UnityEngine;

public interface IAttackSource : IDamageable
{
    Transform Origin { get; }
    Transform TargetOrigin { get; }
    Health TargetHealth { get; }
    float DistanceToTarget { get; }
}
