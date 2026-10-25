using System;
using UnityEngine;

[Serializable]
public class AttackStats
{
    public AttackType type = AttackType.Melee;
    public float damage = 5f;
    public float range = 0.6f;
    public float cooldown = 1f;
    public Projectile projectilePrefab;
    public float projectileSpeed = 6f;
}
