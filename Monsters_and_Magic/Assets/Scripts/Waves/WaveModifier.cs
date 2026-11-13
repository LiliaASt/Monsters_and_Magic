using UnityEngine;

public abstract class WaveModifier : ScriptableObject
{
    public abstract bool Applies(int waveNumber);
    public abstract void Apply(WaveState wave);
}
