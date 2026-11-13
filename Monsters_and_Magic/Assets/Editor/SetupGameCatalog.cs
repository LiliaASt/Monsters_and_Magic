using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the central GameCatalog in Resources from the existing content
/// (heroes, locations, presentation defaults) and offers a one-click
/// "create new enemy" wizard. Idempotent.
/// </summary>
public static class SetupGameCatalog
{
    [MenuItem("Tools/Monsters and Magic/Setup Game Catalog")]
    public static void Setup()
    {
        EnsureFolder("Assets/Resources");

        GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(
            "Assets/Resources/GameCatalog.asset");

        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<GameCatalog>();
            AssetDatabase.CreateAsset(catalog, "Assets/Resources/GameCatalog.asset");
        }

        catalog.heroes = AssetDatabase.FindAssets("t:HeroConfig")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<HeroConfig>)
            .Where(hero => hero != null)
            .ToArray();

        // One location per existing stage: reuse its wave rules and the
        // shared meadow map for now.
        MapConfig map = AssetDatabase.LoadAssetAtPath<MapConfig>(
            "Assets/Configs/World/DefaultMapConfig.asset");
        var locations = new System.Collections.Generic.List<LocationConfig>();

        foreach (string path in AssetDatabase.FindAssets("t:StageConfig"))
        {
            StageConfig stage = AssetDatabase.LoadAssetAtPath<StageConfig>(
                AssetDatabase.GUIDToAssetPath(path));

            if (stage == null)
                continue;

            string locationPath = Path.GetDirectoryName(path)
                is string dir && !string.IsNullOrEmpty(dir) ? dir : "Assets/Configs";
            locationPath = locationPath
                + "/" + stage.name + "Location.asset";
            LocationConfig location = AssetDatabase.LoadAssetAtPath<LocationConfig>(locationPath);

            if (location == null)
            {
                location = ScriptableObject.CreateInstance<LocationConfig>();
                AssetDatabase.CreateAsset(location, locationPath);
            }

            location.locationName = stage.name;
            location.map = map;
            location.waves = stage.waveRules;
            stage.location = location;
            EditorUtility.SetDirty(stage);
            EditorUtility.SetDirty(location);
            locations.Add(location);
        }

        catalog.locations = locations.ToArray();
        catalog.presentation.shadowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Art/Props/ShadowBlob.png");
        catalog.presentation.shadowScale = 0.32f;
        catalog.presentation.xpOrbPrefab = AssetDatabase.LoadAssetAtPath<XPOrb>(
            "Assets/Prefabs/World/XPOrb.prefab");
        EditorUtility.SetDirty(catalog);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Game catalog ready: " + catalog.heroes.Length + " heroes, "
                  + catalog.locations.Length + " locations.");
    }

    [MenuItem("Assets/Create/Monsters and Magic/New Enemy")]
    public static void CreateEnemy()
    {
        string folder = AssetDatabase.GetAssetPath(Selection.activeObject);

        if (string.IsNullOrEmpty(folder))
            folder = "Assets/Configs";

        if (!AssetDatabase.IsValidFolder(folder))
            folder = Path.GetDirectoryName(folder).Replace('\\', '/');

        string configPath = AssetDatabase.GenerateUniqueAssetPath(folder + "/NewEnemy.asset");
        EnemyConfig config = ScriptableObject.CreateInstance<EnemyConfig>();
        AssetDatabase.CreateAsset(config, configPath);

        GameObject root = new GameObject("NewEnemy");
        root.AddComponent<Rigidbody2D>();
        root.AddComponent<CircleCollider2D>();
        SpriteRenderer sprite = root.AddComponent<SpriteRenderer>();
        sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Art/Props/ShadowBlob.png"); // placeholder, replace in inspector
        root.AddComponent<EnemyActor>();
        root.AddComponent<PaperCharacterPresentation>();

        string prefabPath = AssetDatabase.GenerateUniqueAssetPath(folder + "/NewEnemy.prefab");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        config.prefab = prefab.GetComponent<EnemyActor>();
        EditorUtility.SetDirty(config);

        Selection.activeObject = config;
        Debug.Log("New enemy created: " + configPath
                  + " - assign a sprite and stats, everything else is automatic.");
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
