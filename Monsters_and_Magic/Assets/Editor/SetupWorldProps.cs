using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Turns the generated prop sprites (Assets/Art/Props/Prop_*.png) into
/// billboarded world prefabs with a small ground-level collider, and wires
/// them into the chunk-streamed MapConfig. Idempotent.
/// </summary>
public static class SetupWorldProps
{
    private const string PropFolder = "Assets/Art/Props";
    private const string PrefabFolder = "Assets/Prefabs/World/Props";
    private const string ConfigPath = "Assets/Configs/World/DefaultMapConfig.asset";

    [MenuItem("Tools/Monsters and Magic/Setup World Props")]
    public static void Setup()
    {
        EnsureFolder(PrefabFolder);

        var entries = new System.Collections.Generic.List<MapConfig.WorldPropEntry>();

        foreach (string file in Directory.GetFiles(PropFolder, "Prop_*.png"))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            Sprite sprite = ImportPropSprite(file);
            GameObject prefab = CreatePropPrefab(name, sprite);

            entries.Add(new MapConfig.WorldPropEntry
            {
                prefab = prefab,
                probability = 0.16f
            });
        }

        MapConfig config = AssetDatabase.LoadAssetAtPath<MapConfig>(ConfigPath);

        if (config == null)
            throw new System.InvalidOperationException("MapConfig not found");

        config.worldProps = entries.ToArray();
        config.propNoiseScale = 0.06f;
        config.propNoiseThreshold = 0.45f;
        config.propClearRadius = 6f;
        EditorUtility.SetDirty(config);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("World props setup complete: " + entries.Count + " props.");
    }

    private static Sprite ImportPropSprite(string path)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);

        if (importer == null)
            throw new System.InvalidOperationException("Prop not imported: " + path);

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        if (texture == null)
        {
            importer.SaveAndReimport();
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // Normalized height: every prop stands about 2.8 world units tall,
        // anchored at its bottom (the pivot) so it grows "up" from the cell.
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = Mathf.Max(64f, texture.height / 2.8f);
        importer.spritePivot = new Vector2(0.5f, 0f);
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static GameObject CreatePropPrefab(string name, Sprite sprite)
    {
        string path = PrefabFolder + "/" + name + ".prefab";

        GameObject root = new GameObject(name);
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 10;

        root.AddComponent<Billboard>();
        root.AddComponent<YSort>();

        // Ground-level collider far below the sprite top: the hero bumps
        // into the trunk but can run behind the tall crown.
        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.55f, 0.4f);
        collider.offset = new Vector2(0f, 0.2f);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
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
