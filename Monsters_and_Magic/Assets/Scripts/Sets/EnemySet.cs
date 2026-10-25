using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemySet", menuName = "Monsters and Magic/Enemy Set")]
public class EnemySet : ScriptableObject
{
    private readonly List<EnemyActor> items = new();
    public IReadOnlyList<EnemyActor> Items => items;

    public void Add(EnemyActor enemy)
    {
        items.Add(enemy);
    }

    public void Remove(EnemyActor enemy)
    {
        items.Remove(enemy);
    }
}
