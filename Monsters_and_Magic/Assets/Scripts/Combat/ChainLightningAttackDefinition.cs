using UnityEngine;

[CreateAssetMenu(fileName = "ChainLightningAttack", menuName = "Monsters and Magic/Attacks/Chain Lightning")]
public class ChainLightningAttackDefinition : AttackDefinition
{
    [Min(1)] public int chainTargets = 3;
    [Min(0.1f)] public float chainRadius = 2.5f;
    public GameObject hitEffect;
    public GameObject arcPrefab;
    [Min(0.01f)] public float arcLifetime = 0.22f;
    [Range(2, 32)] public int arcSegments = 12;
    [Min(0f)] public float arcJitter = 0.22f;

    public override IAttack Create()
    {
        return new ChainLightningAttack(this);
    }
}
