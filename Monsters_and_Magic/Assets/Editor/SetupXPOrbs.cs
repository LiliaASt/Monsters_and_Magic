using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the shared XP orb prefab (sprite + flat shadow) and assigns it to
/// every enemy prefab. Idempotent.
/// </summary>
public static class SetupXPOrbs
{
    private const string SpritePath = "Assets/Art/Props/XPOrb.png";
    private const string PrefabPath = "Assets/Prefabs/World/XPOrb.prefab";

    [MenuItem("Tools/Monsters and Magic/Setup XP Orbs")]
    public static void Setup()
    {
        ImportOrbSprite();
        GameObject prefab = CreateOrbPrefab();
        AssignToEnemies(prefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("XP orb setup complete.");
    }

    private static void ImportOrbSprite()
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);

        if (importer == null)
            throw new System.InvalidOperationException("XP orb sprite not found");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 128f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    private static GameObject CreateOrbPrefab()
    {
        EnsureFolder("Assets/Prefabs/World");
        Sprite orbSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        Sprite shadowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Art/Props/ShadowBlob.png");

        GameObject root = new GameObject("XPOrb");

        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.linearDamping = 2.5f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D pickup = root.AddComponent<CircleCollider2D>();
        pickup.isTrigger = true;
        pickup.radius = 0.35f;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        SpriteRenderer orbRenderer = visual.AddComponent<SpriteRenderer>();
        orbRenderer.sprite = orbSprite;
        orbRenderer.sortingOrder = 950;
        orbRenderer.color = new Color(0.45f, 1.5f, 2.3f, 1f);

        XPOrb orb = root.AddComponent<XPOrb>();
        Billboard billboard = root.AddComponent<Billboard>();
        billboard.SetVisual(visual.transform);
        SerializedObject serialized = new SerializedObject(orb);
        serialized.FindProperty("visual").objectReferenceValue = visual.transform;
        serialized.FindProperty("sprite").objectReferenceValue = orbRenderer;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void AssignToEnemies(GameObject prefab)
    {
        foreach (string enemy in new[]
                 {
                     "Assets/Prefabs/Entities/Grunt.prefab",
                     "Assets/Prefabs/Entities/Runner.prefab",
                     "Assets/Prefabs/Entities/Brute.prefab"
                 })
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(enemy);
            EnemyActor actor = contents.GetComponent<EnemyActor>();
            SerializedObject serialized = new SerializedObject(actor);
            serialized.FindProperty("orbPrefab").objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(contents, enemy);
            PrefabUtility.UnloadPrefabContents(contents);
        }
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
