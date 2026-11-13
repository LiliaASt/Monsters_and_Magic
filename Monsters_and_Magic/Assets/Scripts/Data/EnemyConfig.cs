using UnityEngine;

[CreateAssetMenu(fileName = "Enemy", menuName = "Monsters and Magic/Enemies/Enemy")]
public class EnemyConfig : ScriptableObject
{
    public string displayName = "Enemy";
    public EnemyActor prefab;
    public float maxHealth = 20f;
    public float moveSpeed = 1f;
    public AttackDefinition attack;
    public int xpReward = 2;
    public GameObject deathEffect;
}
