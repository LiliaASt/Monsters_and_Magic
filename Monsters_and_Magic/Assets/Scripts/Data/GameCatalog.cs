using System;
using UnityEngine;

/// <summary>
/// Central content catalog: heroes, locations and shared presentation
/// defaults. Lives in Resources so runtime systems (enemies, spawners) can
/// pull shared sprites and prefabs without per-prefab wiring.
/// </summary>
[CreateAssetMenu(fileName = "GameCatalog", menuName = "Monsters and Magic/Game Catalog")]
public class GameCatalog : ScriptableObject
{
    [Serializable]
    public class PresentationSettings
    {
        [Tooltip("Blob shadow sprite used under characters.")]
        public Sprite shadowSprite;

        [Tooltip("Relative to the character sprite width.")]
        public float shadowScale = 0.32f;

        [Tooltip("XP orb dropped by enemies if their own prefab is unset.")]
        public XPOrb xpOrbPrefab;
    }

    [Serializable]
    public class OrbSettings
    {
        [Tooltip("XP value of the small tier.")]
        public int smallXp = 1;

        [Tooltip("XP value of the medium tier.")]
        public int mediumXp = 8;

        [Tooltip("XP value of the large tier.")]
        public int largeXp = 25;

        public Color smallColor = new Color(0.55f, 2.4f, 3.2f, 1f);
        public Color mediumColor = new Color(0.55f, 2.8f, 1.4f, 1f);
        public Color largeColor = new Color(2.8f, 0.9f, 3.4f, 1f);

        public float smallScale = 0.26f;
        public float mediumScale = 0.34f;
        public float largeScale = 0.45f;
    }

    [Header("Content")]
    public HeroConfig[] heroes = Array.Empty<HeroConfig>();
    public LocationConfig[] locations = Array.Empty<LocationConfig>();

    [Header("Shared presentation")]
    public PresentationSettings presentation = new PresentationSettings();

    [Header("XP orbs (shards)")]
    public OrbSettings orbs = new OrbSettings();

    public static GameCatalog Load()
    {
        return Resources.Load<GameCatalog>("GameCatalog");
    }
}
