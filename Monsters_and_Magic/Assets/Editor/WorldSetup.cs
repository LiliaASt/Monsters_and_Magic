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

    // The whole ground image becomes ONE tile (no slicing), scaled to cover
    // exactly TileWorldSize world units whatever its pixel size.
    private const int TileWorldSize = 6;

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
        importer.spriteImportMode = SpriteImportMode.Single;
        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        int pixels = source != null ? source.height : 1024;
        importer.spritePixelsPerUnit = (float)pixels / TileWorldSize;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.isReadable = true;

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        importer.SaveAndReimport();
    }

    private static List<Tile> CreateTiles()
    {
        EnsureFolder(TileFolder);
        EnsureFolder(TileFolder + "/Tiles");

        // Remove sliced leftovers from the previous setup so the folder
        // holds exactly one whole-image tile.
        foreach (string old in AssetDatabase.FindAssets("t:Tile", new[] { TileFolder + "/Tiles" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(old);

            if (path.EndsWith(".asset"))
                AssetDatabase.DeleteAsset(path);
        }

        List<Tile> tiles = new List<Tile>();

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath);

        if (sprite == null)
            throw new InvalidOperationException("Meadow sprite not found after import.");

        string tilePath = TileFolder + "/Tiles/" + sprite.name + ".asset";
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);

        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, tilePath);
        }

        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.None;
        EditorUtility.SetDirty(tile);
        tiles.Add(tile);

        if (tiles.Count == 0)
            throw new InvalidOperationException("Meadow tile was not created.");

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

        // The whole image is one self-contained ground tile - no slicing,
        // no separate decoration layer for now (flowers live in the image).
        config.seed = 1337;
        config.chunkSize = 4;
        config.loadRadius = 1;
        config.groundNoiseScale = 0.09f;
        config.groundTiles = tiles.ToArray();
        config.decorations = new MapConfig.DecorationEntry[0];
        config.obstaclesEnabled = false;
        config.obstacles = new MapConfig.ObstacleEntry[0];
        config.obstacleFreeRadius = 4f;
        EditorUtility.SetDirty(config);
        return config;
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

        grid.cellSize = new Vector3(TileWorldSize, TileWorldSize, 1f);

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
