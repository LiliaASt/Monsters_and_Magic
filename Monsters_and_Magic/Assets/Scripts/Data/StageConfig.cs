using UnityEngine;

public enum StageGoalType
{
    KillQuota,
    SurviveTimer,
    CompleteWaves
}

[CreateAssetMenu(fileName = "Stage", menuName = "Monsters and Magic/Stage")]
public class StageConfig : ScriptableObject
{
    public StageGoalType goal = StageGoalType.CompleteWaves;
    public int killQuota = 20;
    public float surviveSeconds = 60f;

    [Tooltip("Full location package (map + waves). Overrides waveRules when set.")]
    public LocationConfig location;

    public WaveRules waveRules;

    [Min(0)]
    public int waveCount = 6;

    [Header("Этапное развитие")]
    [Tooltip("Опыт до первого уровня.")]
    public float levelBaseXp = 12f;

    [Tooltip("Экспоненциальный рост порога опыта (обычно 1.3-1.5).")]
    public float levelGrowth = 1.35f;
}
