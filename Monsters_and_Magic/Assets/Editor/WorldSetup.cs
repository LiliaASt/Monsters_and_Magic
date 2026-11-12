using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// One-click world setup: imports the night meadow texture as tile
//  variations, creates tile assets and a MapConfig, and builds the
/// World/Grid/Ground/Decorations/Obstacles hierarchy with a ChunkManager
/// in the Game scene. Safe to run repeatedly - it updates, never duplicates.
/// </summary>
public static class WorldSetup
{
    private const string SourceTexture =
        "/home/ridrapsody/Downloads/Ночная магическая лесная поляна без деревьев.png";

    private const string TextureFolder = "Assets/Art/Tilemap";
    private const string TexturePath = TextureFolder + "/MagicMeadow.png";
    private const string TileFolder = "Assets/Tilemaps/MagicMeadow";
    private const string ConfigFolder = "Assets/Configs/World";
    private const string ConfigPath = ConfigFolder + "/DefaultMapConfig.asset";
    private const string ScenePath = "Assets/Scenes/Game.unity";

    private const int TileSize = 128;
    private const int DecorationCount = 12;

    [MenuItem("Tools/Monsters and Magic/Setup World Tilemap")]
    public static void Setup()
    {
        ImportMeadowTexture();
        List<Tile> tiles = CreateTiles();
        MapConfig config = CreateMapConfig(tiles);
        SetupScene(config);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("World tilemap setup complete.");
    }

    private static void ImportMeadowTexture()
    {
        EnsureFolder(TextureFolder);

        if (!File.Exists(TexturePath))
        {
            if (!File.Exists(SourceTexture))
                throw new FileNotFoundException("Source meadow texture not found: " + SourceTexture);

            File.Copy(SourceTexture, TexturePath);
            AssetDatabase.ImportAsset(TexturePath);
        }

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = TileSize;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.isReadable = true;
        importer.spritesheet = null;

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        int columns = texture.width / TileSize;
        int rows = texture.height / TileSize;
        List<SpriteMetaData> sheet = new List<SpriteMetaData>();

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
                sheet.Add(new SpriteMetaData
                {
                    name = "tile_" + x + "_" + y,
                    rect = new Rect(x * TileSize, y * TileSize, TileSize, TileSize),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                });
        }

        importer.spritesheet = sheet.ToArray();
        importer.SaveAndReimport();
    }

    private static List<Tile> CreateTiles()
    {
        EnsureFolder(TileFolder);
        EnsureFolder(TileFolder + "/Tiles");

        List<Tile> tiles = new List<Tile>();

        foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(TexturePath).OfType<Sprite>())
        {
            string path = TileFolder + "/Tiles/" + sprite.name + ".asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);

            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }

            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            tiles.Add(tile);
        }

        if (tiles.Count == 0)
            throw new InvalidOperationException("No meadow sprites found after import.");

        return tiles;
    }

    private static MapConfig CreateMapConfig(List<Tile> tiles)
    {
        EnsureFolder(ConfigFolder);

        MapConfig config = AssetDatabase.LoadAssetAtPath<MapConfig>(ConfigPath);

        if (config == null)
        {
            config = ScriptableObject.CreateInstance<MapConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
        }

        // Split tiles by pixel variance: busy, sparkly pieces become
        // decoration patches, calm even pieces stay as ground.
        List<Tile> decorations = new List<Tile>(tiles);
        decorations.Sort((a, b) =>
            MeasureVariance(b.sprite).CompareTo(MeasureVariance(a.sprite)));
        decorations = decorations.GetRange(0, Mathf.Min(DecorationCount, decorations.Count));

        List<Tile> ground = new List<Tile>(tiles);
        foreach (Tile decoration in decorations)
            ground.Remove(decoration);

        config.seed = 1337;
        config.chunkSize = 16;
        config.loadRadius = 2;
        config.groundNoiseScale = 0.09f;
        config.groundTiles = ground.ToArray();

        List<MapConfig.DecorationEntry> entries = new List<MapConfig.DecorationEntry>();

        foreach (Tile decoration in decorations)
        {
            entries.Add(new MapConfig.DecorationEntry
            {
                tile = decoration,
                probability = 0.05f,
                noiseScale = 0.05f,
                noiseThreshold = 0.55f
            });
        }

        config.decorations = entries.ToArray();
        config.obstaclesEnabled = false;
        config.obstacles = new MapConfig.ObstacleEntry[0];
        config.obstacleFreeRadius = 4f;
        EditorUtility.SetDirty(config);
        return config;
    }

    private static float MeasureVariance(Sprite sprite)
    {
        Texture2D texture = sprite.texture;
        Rect rect = sprite.textureRect;
        int step = 8;
        float sum = 0f;
        float sumSquares = 0f;
        int count = 0;

        for (int y = (int)rect.y; y < rect.y + rect.height; y += step)
        {
            for (int x = (int)rect.x; x < rect.x + rect.width; x += step)
            {
                Color color = texture.GetPixel(x, y);
                float luma = color.r * 0.3f + color.g * 0.59f + color.b * 0.11f;
                sum += luma;
                sumSquares += luma * luma;
                count++;
            }
        }

        if (count == 0)
            return 0f;

        float mean = sum / count;
        return sumSquares / count - mean * mean;
    }

    private static void SetupScene(MapConfig config)
    {
        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        bool sceneLoaded = EditorSceneManager.GetActiveScene().path == ScenePath;

        if (!sceneLoaded)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Transform hero = GameObject.Find("HeroActor") != null
            ? GameObject.Find("HeroActor").transform
            : null;

        GameObject world = GetOrCreate("World");
        GameObject gridObject = GetOrCreateChild(world.transform, "Grid");
        Grid grid = gridObject.GetComponent<Grid>();

        if (grid == null)
            grid = gridObject.AddComponent<Grid>();

        grid.cellSize = Vector3.one;

        Tilemap ground = GetOrCreateTilemap(gridObject.transform, "Ground", -10);
        Tilemap decorations = GetOrCreateTilemap(gridObject.transform, "Decorations", -9);
        Tilemap obstacles = GetOrCreateTilemap(gridObject.transform, "Obstacles", -8);

        if (obstacles.GetComponent<TilemapCollider2D>() == null)
            obstacles.gameObject.AddComponent<TilemapCollider2D>();

        GameObject managerObject = GetOrCreateChild(world.transform, "ChunkManager");
        ChunkManager manager = managerObject.GetComponent<ChunkManager>();

        if (manager == null)
            manager = managerObject.AddComponent<ChunkManager>();

        SerializedObject serialized = new SerializedObject(manager);
        serialized.FindProperty("config").objectReferenceValue = config;
        serialized.FindProperty("ground").objectReferenceValue = ground;
        serialized.FindProperty("decorations").objectReferenceValue = decorations;
        serialized.FindProperty("obstacles").objectReferenceValue = obstacles;
        serialized.FindProperty("followTarget").objectReferenceValue = hero;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        config.obstacleClearCenter = hero != null
            ? (Vector2)hero.position
            : Vector2.zero;
        EditorUtility.SetDirty(config);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        if (!sceneLoaded && previousSetup != null && previousSetup.Length > 0)
            EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
    }

    private static GameObject GetOrCreate(string name)
    {
        GameObject existing = GameObject.Find(name);
        return existing != null ? existing : new GameObject(name);
    }

    private static GameObject GetOrCreateChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);

        if (existing != null)
            return existing.gameObject;

        GameObject created = new GameObject(name);
        created.transform.SetParent(parent, false);
        return created;
    }

    private static Tilemap GetOrCreateTilemap(Transform parent, string name, int sortingOrder)
    {
        GameObject tilemapObject = GetOrCreateChild(parent, name);
        Tilemap map = tilemapObject.GetComponent<Tilemap>();

        if (map == null)
            map = tilemapObject.AddComponent<Tilemap>();

        TilemapRenderer renderer = tilemapObject.GetComponent<TilemapRenderer>();

        if (renderer == null)
            renderer = tilemapObject.AddComponent<TilemapRenderer>();

        renderer.sortingOrder = sortingOrder;
        return map;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, leaf);
    }
}
