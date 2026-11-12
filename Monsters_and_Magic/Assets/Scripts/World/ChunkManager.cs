using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Streams tilemap chunks around the hero. Owns which chunks are loaded and
/// their budget per frame; all actual tile picking lives in ChunkGenerator.
/// One shared Grid with three Tilemap layers - no per-chunk GameObjects.
/// </summary>
public class ChunkManager : MonoBehaviour
{
    [SerializeField] private MapConfig config;
    [SerializeField] private Tilemap ground;
    [SerializeField] private Tilemap decorations;
    [SerializeField] private Tilemap obstacles;
    [SerializeField] private Transform followTarget;
    [SerializeField, Min(1)] private int maxChunksPerFrame = 4;

    public static ChunkManager Instance { get; private set; }

    private readonly Dictionary<Vector2Int, bool> loaded = new();
    private readonly List<Vector2Int> pending = new();
    private readonly List<Vector2Int> scratch = new();

    private TileBase[] groundBuffer;
    private TileBase[] decorationBuffer;
    private TileBase[] obstacleBuffer;
    private Vector2Int currentChunk;
    private bool hasCurrentChunk;

    private void Awake()
    {
        Instance = this;

        if (config != null)
        {
            int count = config.chunkSize * config.chunkSize;
            groundBuffer = new TileBase[count];
            decorationBuffer = new TileBase[count];
            obstacleBuffer = new TileBase[count];
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void LateUpdate()
    {
        if (config == null || followTarget == null)
            return;

        Vector2Int chunk = WorldToChunk(followTarget.position);

        if (!hasCurrentChunk || chunk != currentChunk)
        {
            currentChunk = chunk;
            hasCurrentChunk = true;
            RefreshDesiredChunks();
        }

        LoadPendingChunks();
    }

    private Vector2Int WorldToChunk(Vector3 worldPosition)
    {
        int size = config.chunkSize;
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / size),
            Mathf.FloorToInt(worldPosition.y / size));
    }

    private void RefreshDesiredChunks()
    {
        int radius = config.loadRadius;

        // Unload everything outside the desired square.
        scratch.Clear();

        foreach (KeyValuePair<Vector2Int, bool> pair in loaded)
        {
            Vector2Int delta = pair.Key - currentChunk;

            if (Mathf.Abs(delta.x) > radius || Mathf.Abs(delta.y) > radius)
                scratch.Add(pair.Key);
        }

        foreach (Vector2Int chunk in scratch)
        {
            UnloadChunk(chunk);
            loaded.Remove(chunk);
        }

        // Queue missing chunks, closest first so the visible area fills first.
        pending.Clear();

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                Vector2Int chunk = new Vector2Int(currentChunk.x + x, currentChunk.y + y);

                if (!loaded.ContainsKey(chunk))
                    pending.Add(chunk);
            }
        }

        pending.Sort(CompareByDistanceToCurrent);
    }

    private void LoadPendingChunks()
    {
        int budget = Mathf.Min(maxChunksPerFrame, pending.Count);

        for (int i = 0; i < budget; i++)
        {
            Vector2Int chunk = pending[i];

            if (loaded.ContainsKey(chunk))
                continue;

            ChunkGenerator.Generate(chunk, config,
                groundBuffer, decorationBuffer, obstacleBuffer);

            WriteChunk(ground, chunk, groundBuffer);
            WriteChunk(decorations, chunk, decorationBuffer);
            WriteChunk(obstacles, chunk, obstacleBuffer);
            loaded[chunk] = true;
        }

        if (budget > 0)
            pending.RemoveRange(0, budget);
    }

    private void WriteChunk(Tilemap map, Vector2Int chunk, TileBase[] tiles)
    {
        if (map == null)
            return;

        int size = config.chunkSize;
        BoundsInt bounds = new BoundsInt(
            chunk.x * size, chunk.y * size, 0,
            size, size, 1);
        map.SetTilesBlock(bounds, tiles);
    }

    private void UnloadChunk(Vector2Int chunk)
    {
        int size = config.chunkSize;
        int count = size * size;

        if (groundBuffer == null || groundBuffer.Length != count)
        {
            groundBuffer = new TileBase[count];
            decorationBuffer = new TileBase[count];
            obstacleBuffer = new TileBase[count];
        }

        for (int i = 0; i < count; i++)
        {
            groundBuffer[i] = null;
            decorationBuffer[i] = null;
            obstacleBuffer[i] = null;
        }

        WriteChunk(ground, chunk, groundBuffer);
        WriteChunk(decorations, chunk, decorationBuffer);
        WriteChunk(obstacles, chunk, obstacleBuffer);
    }

    private int CompareByDistanceToCurrent(Vector2Int a, Vector2Int b)
    {
        int da = (a.x - currentChunk.x) * (a.x - currentChunk.x)
               + (a.y - currentChunk.y) * (a.y - currentChunk.y);
        int db = (b.x - currentChunk.x) * (b.x - currentChunk.x)
               + (b.y - currentChunk.y) * (b.y - currentChunk.y);
        return da.CompareTo(db);
    }

    /// <summary>True if the world position sits on a generated obstacle tile.</summary>
    public bool IsPositionBlocked(Vector3 worldPosition)
    {
        return obstacles != null
            && config != null
            && config.obstaclesEnabled
            && obstacles.GetTile(obstacles.WorldToCell(worldPosition)) != null;
    }
}
