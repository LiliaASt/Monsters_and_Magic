using UnityEngine;

[CreateAssetMenu(fileName = "Hero_Default", menuName = "Monsters and Magic/Hero Config")]
public class HeroConfig : ScriptableObject
{
    [Header("Общее")]
    public string displayName = "Герой";

    [Header("Характеристики")]
    public float maxHealth = 100f;
    public float regenPerSec = 0f;
    public float moveSpeed = 3f;

    [Header("Атака")]
    public AttackStats attack = new AttackStats
    {
        damage = 10f,
        range = 2f,
        cooldown = 1f
    };
}
