using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// Pure deterministic chunk content generator. The same seed + chunk
/// coordinates + MapConfig always produce the same tiles, no matter in which
/// order chunks are loaded. Never touches UnityEngine.Random, so combat
/// randomness stays independent from the world.
/// </summary>
public static class ChunkGenerator
{
    /// <summary>
    /// Fills the three tile arrays (chunkSize * chunkSize, row by row from
    /// bottom-left) for the given chunk. Entries may be null.
    /// </summary>
    public static void Generate(Vector2Int chunkCoord, MapConfig config,
        TileBase[] ground, TileBase[] decorations, TileBase[] obstacles)
    {
        int size = config.chunkSize;
        int offsetX = chunkCoord.x * size;
        int offsetY = chunkCoord.y * size;

        float seedOffsetX = Hash01(config.seed, 101);
        float seedOffsetY = Hash01(config.seed, 202);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int worldX = offsetX + x;
                int worldY = offsetY + y;
                int index = y * size + x;

                ground[index] = PickGroundTile(config, worldX, worldY,
                    seedOffsetX, seedOffsetY);
                decorations[index] = null;
                obstacles[index] = null;

                if (config.obstaclesEnabled && config.obstacles != null)
                {
                    TileBase obstacle = PickObstacle(config, worldX, worldY);

                    if (obstacle != null)
                    {
                        obstacles[index] = obstacle;
                        continue;
                    }
                }

                decorations[index] = PickDecoration(config, worldX, worldY);
            }
        }
    }

    public struct PropSpawn
    {
        public Vector2 Position;
        public int PropIndex;
        public bool FlipX;
        public float Scale;
        public float Tilt;
    }

    /// <summary>
    /// Deterministic world-space prop spawns for a chunk (one prop per cell
    /// at most). Same seed + coordinates always regrow the same grove.
    /// </summary>
    public static void GeneratePropPositions(Vector2Int chunkCoord, MapConfig config,
        float cellSize, List<PropSpawn> output)
    {
        output.Clear();

        if (config.worldProps == null || config.worldProps.Length == 0)
            return;

        int size = config.chunkSize;
        int offsetX = chunkCoord.x * size;
        int offsetY = chunkCoord.y * size;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int cellX = offsetX + x;
                int cellY = offsetY + y;

                float mask = Mathf.PerlinNoise(
                    cellX * config.propNoiseScale + Hash01(config.seed, 505) * 512f,
                    cellY * config.propNoiseScale + Hash01(config.seed, 606) * 512f);

                if (mask < config.propNoiseThreshold)
                    continue;

                for (int propIndex = 0; propIndex < config.worldProps.Length; propIndex++)
                {
                    MapConfig.WorldPropEntry entry = config.worldProps[propIndex];

                    if (entry.prefab == null || entry.probability <= 0f)
                        continue;

                    if (Hash01(cellX, cellY, config.seed * 31 + 23 + propIndex * 7)
                        >= entry.probability)
                        continue;

                    float jitterX = (Hash01(cellX, cellY, config.seed * 31 + 29) - 0.5f) * 0.5f;
                    float jitterY = (Hash01(cellX, cellY, config.seed * 31 + 31) - 0.5f) * 0.5f;

                    Vector2 position = new Vector2(
                        (cellX + 0.5f + jitterX) * cellSize,
                        (cellY + 0.5f + jitterY) * cellSize);

                    if ((position - config.obstacleClearCenter).sqrMagnitude
                        < config.propClearRadius * config.propClearRadius)
                        continue;

                    output.Add(new PropSpawn
                    {
                        Position = position,
                        PropIndex = propIndex,
                        FlipX = Hash01(cellX, cellY, config.seed * 31 + 41 + propIndex) > 0.5f,
                        Scale = 0.75f + Hash01(cellX, cellY, config.seed * 31 + 43 + propIndex) * 0.65f,
                        Tilt = (Hash01(cellX, cellY, config.seed * 31 + 47 + propIndex) - 0.5f) * 22f
                    });
                    break;
                }
            }
        }
    }

    private static TileBase PickGroundTile(MapConfig config, int x, int y,
        float seedOffsetX, float seedOffsetY)
    {
        if (config.groundTiles == null || config.groundTiles.Length == 0)
            return null;

        // Coherent noise maps neighbouring cells to nearby values, so tile
        // variations form connected patches instead of confetti. A tiny hash
        // jitter breaks up hard banding between neighbour indices.
        float noise = Mathf.PerlinNoise(
            x * config.groundNoiseScale + seedOffsetX * 256f,
            y * config.groundNoiseScale + seedOffsetY * 256f);
        float jitter = Hash01(x, y, config.seed) * 0.08f;
        float value = Mathf.Clamp01(noise + jitter - 0.04f);

        return config.groundTiles[
            Mathf.Min(config.groundTiles.Length - 1,
                Mathf.FloorToInt(value * config.groundTiles.Length))];
    }

    private static TileBase PickDecoration(MapConfig config, int x, int y)
    {
        if (config.decorations == null)
            return null;

        foreach (MapConfig.DecorationEntry entry in config.decorations)
        {
            if (entry.tile == null || entry.probability <= 0f)
                continue;

            float mask = Mathf.PerlinNoise(
                x * entry.noiseScale + Hash01(config.seed, 303) * 512f,
                y * entry.noiseScale + Hash01(config.seed, 404) * 512f);

            if (mask < entry.noiseThreshold)
                continue;

            if (Hash01(x, y, config.seed * 31 + 7) < entry.probability)
                return entry.tile;
        }

        return null;
    }

    private static TileBase PickObstacle(MapConfig config, int x, int y)
    {
        Vector2 cell = new Vector2(x + 0.5f, y + 0.5f);

        if ((cell - config.obstacleClearCenter).sqrMagnitude
            < config.obstacleFreeRadius * config.obstacleFreeRadius)
            return null;

        foreach (MapConfig.ObstacleEntry entry in config.obstacles)
        {
            if (entry.tile == null || entry.probability <= 0f)
                continue;

            if (Hash01(x, y, config.seed * 31 + 13) < entry.probability)
                return entry.tile;
        }

        return null;
    }

    /// <summary>Deterministic float hash in [0, 1) from int coordinates.</summary>
    private static float Hash01(int a, int b, int salt = 0)
    {
        unchecked
        {
            // Murmur-style finalizer: well-distributed even for tiny
            // coordinate deltas, unlike the previous weak mix.
            uint hash = (uint)(a * 374761393 + b * 668265263 + salt * 1274126177);
            hash ^= hash >> 13;
            hash *= 1274126177;
            hash ^= hash >> 16;
            return (hash & 0x00ffffffu) / 16777216f;
        }
    }
}
