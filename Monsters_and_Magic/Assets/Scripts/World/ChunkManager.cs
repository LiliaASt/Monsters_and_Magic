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
    [SerializeField] private Transform propContainer;

    public static ChunkManager Instance { get; private set; }

    private readonly Dictionary<Vector2Int, bool> loaded = new();
    private readonly List<Vector2Int> pending = new();
    private readonly List<Vector2Int> scratch = new();

    private TileBase[] groundBuffer;
    private TileBase[] decorationBuffer;
    private TileBase[] obstacleBuffer;
    private readonly Dictionary<Vector2Int, List<GameObject>> propChunks = new();
    private readonly List<ChunkGenerator.PropSpawn> propSpawns = new();
    private Vector2Int currentChunk;
    private bool hasCurrentChunk;
    private float cellSizeX = 1f;
    private float cellSizeY = 1f;

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

        if (ground != null && ground.layoutGrid != null)
        {
            cellSizeX = Mathf.Max(0.01f, ground.layoutGrid.cellSize.x);
            cellSizeY = Mathf.Max(0.01f, ground.layoutGrid.cellSize.y);
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
        // World units -> grid cells -> chunk index. With a 6-unit cell a
        // chunk of 4 cells spans 24 world units; dividing world position by
        // chunkSize directly made the current chunk flip every few steps and
        // left holes at the loaded-area border.
        int cellX = Mathf.FloorToInt(worldPosition.x / cellSizeX);
        int cellY = Mathf.FloorToInt(worldPosition.y / cellSizeY);
        int size = config.chunkSize;
        return new Vector2Int(
            Mathf.FloorToInt((float)cellX / size),
            Mathf.FloorToInt((float)cellY / size));
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
            SpawnProps(chunk);
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
        DespawnProps(chunk);
    }

    private void SpawnProps(Vector2Int chunk)
    {
        if (config.worldProps == null || config.worldProps.Length == 0)
            return;

        ChunkGenerator.GeneratePropPositions(chunk, config, cellSizeX, propSpawns);

        if (propSpawns.Count == 0)
            return;

        List<GameObject> props = new List<GameObject>(propSpawns.Count);

        foreach (ChunkGenerator.PropSpawn spawn in propSpawns)
        {
            MapConfig.WorldPropEntry entry = config.worldProps[spawn.PropIndex];

            if (entry.prefab == null)
                continue;

            GameObject prop = Instantiate(
                entry.prefab,
                new Vector3(spawn.Position.x, spawn.Position.y, 0f),
                Quaternion.identity,
                propContainer != null ? propContainer : transform);
            prop.transform.localScale = new Vector3(
                spawn.Scale * (spawn.FlipX ? -1f : 1f), spawn.Scale, 1f);

            Billboard billboard = prop.GetComponent<Billboard>();

            if (billboard != null)
                billboard.SetTilt(spawn.Tilt);

            props.Add(prop);
        }

        propChunks[chunk] = props;
        float minScale = float.MaxValue;
        float maxScale = float.MinValue;

        foreach (ChunkGenerator.PropSpawn spawn in propSpawns)
        {
            minScale = Mathf.Min(minScale, spawn.Scale);
            maxScale = Mathf.Max(maxScale, spawn.Scale);
        }


    }

    private void DespawnProps(Vector2Int chunk)
    {
        if (!propChunks.TryGetValue(chunk, out List<GameObject> props))
            return;

        foreach (GameObject prop in props)
        {
            if (prop != null)
                Destroy(prop);
        }

        props.Clear();
        propChunks.Remove(chunk);
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
