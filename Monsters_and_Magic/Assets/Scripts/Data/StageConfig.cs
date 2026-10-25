using UnityEngine;

[CreateAssetMenu(fileName = "Stage_1", menuName = "Monsters and Magic/Stage Config")]
public class StageConfig : ScriptableObject
{
    [Header("Цель этапа")]
    public int killQuota = 20;
}
