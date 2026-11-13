public interface IAttack
{
    void Initialize(IAttackSource source, AttackInstance attack);
    void Tick();
}
