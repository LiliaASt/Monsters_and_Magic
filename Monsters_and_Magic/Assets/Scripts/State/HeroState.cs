using System;

[Serializable]
public class HeroState
{
    public string id;
    public int globalLevel = 1;
    public int completedLevels;
    public int currentLevel = 1;
    public int wins;
    public int deaths;
    public int totalKills;
}
