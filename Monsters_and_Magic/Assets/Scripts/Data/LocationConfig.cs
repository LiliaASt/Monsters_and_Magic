using UnityEngine;

/// <summary>
/// A playable location: pairs a procedurally generated map with its wave
/// rules and spawn tables. New locations are plain assets - no code.
/// </summary>
[CreateAssetMenu(fileName = "Location", menuName = "Monsters and Magic/Location")]
public class LocationConfig : ScriptableObject
{
    public string locationName = "Night Meadow";

    [Tooltip("Ground, props and chunk streaming rules.")]
    public MapConfig map;

    [Tooltip("Waves, spawn tables, portals and modifiers for this location.")]
    public WaveRules waves;
}
