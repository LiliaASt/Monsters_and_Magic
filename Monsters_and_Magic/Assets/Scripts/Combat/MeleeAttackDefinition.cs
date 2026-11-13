using UnityEngine;

[CreateAssetMenu(fileName = "MeleeAttack", menuName = "Monsters and Magic/Attacks/Melee")]
public class MeleeAttackDefinition : AttackDefinition
{
    public GameObject hitEffect;

    public override IAttack Create()
    {
        return new MeleeAttack(this);
    }
}
