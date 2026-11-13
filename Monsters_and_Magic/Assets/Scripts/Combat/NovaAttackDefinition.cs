using UnityEngine;

[CreateAssetMenu(fileName = "NovaAttack", menuName = "Monsters and Magic/Attacks/Nova")]
public class NovaAttackDefinition : AttackDefinition
{
    [Min(0.1f)] public float radius = 2.5f;
    public GameObject castEffect;

    public override IAttack Create()
    {
        return new NovaAttack(radius, castEffect);
    }
}
