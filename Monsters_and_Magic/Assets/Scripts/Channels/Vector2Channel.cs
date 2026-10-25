using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Vector2Channel", menuName = "Monsters and Magic/Channels/Vector2 Channel")]
public class Vector2Channel : ScriptableObject
{
    private event Action<Vector2> Raised;

    public void Subscribe(Action<Vector2> listener)
    {
        Raised += listener;
    }

    public void Unsubscribe(Action<Vector2> listener)
    {
        Raised -= listener;
    }

    public void Publish(Vector2 value)
    {
        Raised?.Invoke(value);
    }
}
