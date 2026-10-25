using UnityEngine;

public enum StageGoalType
{
    KillQuota,
    SurviveTimer
}

[CreateAssetMenu(fileName = "Stage_1", menuName = "Monsters and Magic/Stage Config")]
public class StageConfig : ScriptableObject
{
    [Header("Цель этапа")]
    public StageGoalType goal = StageGoalType.KillQuota;

    [Header("KillQuota: убить N врагов")]
    public int killQuota = 20;

    [Header("SurviveTimer: выжить N секунд")]
    public float surviveSeconds = 60f;
}
