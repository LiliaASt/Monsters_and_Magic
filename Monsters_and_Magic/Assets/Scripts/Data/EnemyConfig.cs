using UnityEngine;

[CreateAssetMenu(fileName = "Enemy_Normal", menuName = "Monsters and Magic/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    [Header("Общее")]
    public string displayName = "Обычный";

    [Header("Характеристики")]
    public float maxHealth = 20f;
    public float moveSpeed = 1f;

    [Header("Атака")]
    public AttackStats attack = new AttackStats
    {
        damage = 5f,
        range = 0.6f,
        cooldown = 1f
    };

    [Header("Награда")]
    public int xpReward = 2;
    public int crystalReward = 1;
    [Range(0f, 1f)] public float crystalDropChance = 0.3f;
}
