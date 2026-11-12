using System;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Data-only description of one procedural map: tiles, noise settings and
/// spawn rules. Different maps (forest, desert, dungeon) are different
/// assets of this class - never different ChunkManager implementations.
/// </summary>
[CreateAssetMenu(fileName = "MapConfig", menuName = "Monsters and Magic/World/Map Config")]
public class MapConfig : ScriptableObject
{
    [Header("Chunks")]
    [Min(4)]
    public int chunkSize = 16;

    [Min(1)]
    public int loadRadius = 2;

    [Header("Generation")]
    public int seed = 1337;

    [Tooltip("Higher = smaller, busier ground variation patches.")]
    [Range(0.02f, 0.5f)]
    public float groundNoiseScale = 0.09f;

    [Header("Ground")]
    [Tooltip("Base ground tiles. Variations are picked by coherent noise so neighbours form connected patches.")]
    public TileBase[] groundTiles;

    [Serializable]
    public struct DecorationEntry
    {
        public TileBase tile;

        [Range(0f, 1f)]
        [Tooltip("Chance per cell once the cluster noise mask passes.")]
        public float probability;

        [Tooltip("Cluster mask noise scale - keeps decorations in natural patches.")]
        public float noiseScale;

        [Range(0f, 1f)]
        [Tooltip("Cluster noise must exceed this for the decoration to appear.")]
        public float noiseThreshold;
    }

    [Header("Decorations")]
    public DecorationEntry[] decorations;

    [Serializable]
    public struct ObstacleEntry
    {
        public TileBase tile;

        [Range(0f, 1f)]
        public float probability;
    }

    [Header("Obstacles")]
    [Tooltip("Off by default: enemies walk straight at the hero, dense blockers would make them stuck.")]
    public bool obstaclesEnabled;

    public ObstacleEntry[] obstacles;

    [Tooltip("No obstacles are generated inside this radius around the clear center.")]
    [Min(0f)]
    public float obstacleFreeRadius = 4f;

    public Vector2 obstacleClearCenter = Vector2.zero;
}
