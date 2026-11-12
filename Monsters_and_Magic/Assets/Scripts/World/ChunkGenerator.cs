using UnityEngine;
using UnityEngine.Tilemaps;

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
            uint hash = (uint)salt;
            hash = (hash ^ 0x9e3779b9u) + (uint)a * 0x85ebca6bu;
            hash = (hash ^ (hash >> 13)) + (uint)b * 0xc2b2ae35u;
            hash = (hash ^ (hash >> 16)) + (uint)salt * 0x27d4eb2fu;
            hash ^= hash >> 15;
            return (hash & 0x00ffffffu) / 16777216f;
        }
    }
}
