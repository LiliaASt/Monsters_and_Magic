using UnityEngine;

public abstract class RunRewardRule : ScriptableObject
{
    [SerializeField] private ResourceConfig resource;

    public ResourceConfig Resource => resource;

    public abstract int GetAmount(RunResult result);
}
