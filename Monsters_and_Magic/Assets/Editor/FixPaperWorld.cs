using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Paper-2.5D fix pass: world prop layers, blob shadows under every
/// character, ground tilemap sorting, projectile layers and the night
/// forest ground texture. Idempotent.
/// </summary>
public static class FixPaperWorld
{
    private const string ScenePath = "Assets/Scenes/Game.unity";
    private const string GroundSource =
        "/home/ridrapsody/Downloads/night_forest_ground_seamless_1024.png";
    private const string GroundTarget = "Assets/Art/Tilemap/MagicMeadow.png";
    private const string ShadowSpritePath = "Assets/Art/Props/ShadowBlob.png";

    [MenuItem("Tools/Monsters and Magic/Fix Paper World")]
    public static void Fix()
    {
        SetupLayers();
        ImportShadowSprite();
        ReplaceGroundTexture();

        // Rebuild the ground tile + config first: WorldSetup assigns its own
        // tilemap sorting orders, so ours must be applied after it.
        WorldSetup.Setup();

        var previousSetup = EditorSceneManager.GetSceneManagerSetup();
        bool sceneLoaded = EditorSceneManager.GetActiveScene().path == ScenePath;

        if (!sceneLoaded)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Ground tilemaps sit far below every Y-sorted sprite.
        foreach (TilemapRenderer renderer in Object.FindObjectsByType<TilemapRenderer>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (renderer != null)
                renderer.sortingOrder = -32000;
        }

        AddShadow(GameObject.Find("HeroActor"), 0.28f);
        MoveSpriteToVisual(GameObject.Find("HeroActor"));
        GameObject hero = GameObject.Find("HeroActor");

        if (hero != null)
            hero.layer = 10;

        AddShadowToPrefab("Assets/Prefabs/Entities/Grunt.prefab", 0.32f);
        AddShadowToPrefab("Assets/Prefabs/Entities/Runner.prefab", 0.28f);
        AddShadowToPrefab("Assets/Prefabs/Entities/Brute.prefab", 0.36f);

        SetLayerForPrefabs("Assets/Prefabs/World/Props", 8);
        // Shadows only under characters, not under trees and rocks.
        RemoveShadowsFromProps("Assets/Prefabs/World/Props");

        SetLayerForPrefabs("Assets/Prefabs/Combat", 9);

        // Wire the level-up presenter on the hero.
        GameObject heroObject = GameObject.Find("HeroActor");

        if (heroObject != null)
        {
            LevelUpPresenter presenter = heroObject.GetComponent<LevelUpPresenter>();

            if (presenter == null)
                presenter = heroObject.AddComponent<LevelUpPresenter>();

            GameObject levelUp = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/VFX/Custom/LevelUpEffect.prefab");
            SerializedObject presenterSerialized = new SerializedObject(presenter);
            presenterSerialized.FindProperty("effectPrefab").objectReferenceValue = levelUp;
            presenterSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);
        }


        // Projectiles: physics rotates the root, the billboard rotates the
        // Visual child - no more fighting over the same transform.
        foreach (string projectile in new[]
                 {
                     "Assets/Prefabs/Combat/HeroProjectile.prefab",
                     "Assets/Prefabs/Combat/MagicMissileProjectile.prefab",
                     "Assets/Prefabs/Combat/Projectile.prefab"
                 })
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(projectile);
            MoveSpriteToVisual(contents);
            NormalizeProjectileScale(contents);
            PrefabUtility.SaveAsPrefabAsset(contents, projectile);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        // Fireball keeps its own 0.18 scale too - same normalization.
        GameObject fireballContents = PrefabUtility.LoadPrefabContents(
            "Assets/Prefabs/Combat/FireballProjectile.prefab");
        NormalizeProjectileScale(fireballContents);
        PrefabUtility.SaveAsPrefabAsset(fireballContents,
            "Assets/Prefabs/Combat/FireballProjectile.prefab");
        PrefabUtility.UnloadPrefabContents(fireballContents);

        foreach (string file in Directory.GetFiles("Assets/Prefabs/World/Props", "*.prefab"))
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(file);
            MoveSpriteToVisual(contents);
            WidenCollider(contents);
            PrefabUtility.SaveAsPrefabAsset(contents, file);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        foreach (string enemy in new[]
                 {
                     "Assets/Prefabs/Entities/Grunt.prefab",
                     "Assets/Prefabs/Entities/Runner.prefab",
                     "Assets/Prefabs/Entities/Brute.prefab"
                 })
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(enemy);

            DamagePopupPresenter presenter =
                contents.GetComponent<DamagePopupPresenter>();

            if (presenter == null)
                presenter = contents.AddComponent<DamagePopupPresenter>();

            SerializedObject actorSerialized = new SerializedObject(
                contents.GetComponent<EnemyActor>());
            actorSerialized.FindProperty("damagePopups").objectReferenceValue = presenter;
            actorSerialized.ApplyModifiedPropertiesWithoutUndo();

            if (contents.GetComponent<PaperCharacterPresentation>() == null)
                contents.AddComponent<PaperCharacterPresentation>();

            // A RequireComponent used to add an empty root SpriteRenderer
            // which then wiped the real sprite - remove such leftovers.
            SpriteRenderer rootSprite = contents.GetComponent<SpriteRenderer>();

            if (rootSprite != null && rootSprite.sprite == null)
                Object.DestroyImmediate(rootSprite, true);

            ApplyEnemyTint(contents, Path.GetFileNameWithoutExtension(enemy));
            MoveSpriteToVisual(contents);
            PrefabUtility.SaveAsPrefabAsset(contents, enemy);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        if (!sceneLoaded && previousSetup != null && previousSetup.Length > 0)
            EditorSceneManager.RestoreSceneManagerSetup(previousSetup);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Paper world fixes complete.");
    }

    private static void SetupLayers()
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        SetLayerName(layers, 8, "WorldProps");
        SetLayerName(layers, 9, "Projectiles");
        SetLayerName(layers, 10, "Player");

        tagManager.ApplyModifiedPropertiesWithoutUndo();

        // Enemies (Default) and projectiles walk through world props so they
        // never get stuck behind trees; the hero (Player) still bumps into
        // trunks. IgnoreLayerCollision is the official matrix API.
        Physics2D.IgnoreLayerCollision(0, 8, true);
        Physics2D.IgnoreLayerCollision(9, 8, true);
        Physics2D.IgnoreLayerCollision(10, 8, false);
    }

    private static void SetLayerName(SerializedProperty layers, int index, string name)
    {
        SerializedProperty slot = layers.GetArrayElementAtIndex(index);

        if (slot.stringValue != name)
            slot.stringValue = name;
    }

    private static void ReplaceGroundTexture()
    {
        if (!File.Exists(GroundSource))
            throw new System.InvalidOperationException("Night forest texture not found: " + GroundSource);

        File.Copy(GroundSource, GroundTarget, overwrite: true);
        AssetDatabase.ImportAsset(GroundTarget);
    }

    private static void AddShadow(GameObject target, float size)
    {
        if (target == null)
            return;

        Transform existing = target.transform.Find("ShadowBlob");
        SpriteRenderer sprite = target.GetComponentInChildren<SpriteRenderer>();

        // Shadow diameter is proportional to the prefab's sprite width.
        float shadowSize = size;

        if (sprite != null && sprite.sprite != null)
            shadowSize = Mathf.Clamp(sprite.sprite.bounds.size.x * size, 0.3f, 1.15f);

        // Anchor the shadow to the sprite's actual bottom (bounds work for
        // both center and bottom pivots), slightly towards the feet.
        float bottomY = 0f;

        if (sprite != null && sprite.sprite != null)
            bottomY = sprite.sprite.bounds.center.y - sprite.sprite.bounds.extents.y;

        if (existing == null)
        {
            GameObject shadow = new GameObject("ShadowBlob");
            shadow.transform.SetParent(target.transform, false);
            SpriteRenderer renderer = shadow.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShadowSpritePath);
            renderer.sortingOrder = -31000;
            shadow.transform.localScale = new Vector3(shadowSize, shadowSize, 1f);
            shadow.transform.localPosition = new Vector3(0f, bottomY + 0.05f, 0f);
        }
        else
        {
            // Update an existing shadow too: the size grew after the first
            // pass, and skipping the update left tiny hidden shadows.
            existing.localScale = new Vector3(shadowSize, shadowSize, 1f);
            existing.localPosition = new Vector3(0f, bottomY + 0.05f, 0f);
            SpriteRenderer renderer = existing.GetComponent<SpriteRenderer>();

            if (renderer != null)
            {
                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShadowSpritePath);
                renderer.sortingOrder = -31000;
            }
        }
    }

    /// <summary>
    /// Moves the flat sprite into a Visual child that the billboard rotates,
    /// leaving the ground shadow child flat in the gameplay plane.
    /// </summary>
    private static void MoveSpriteToVisual(GameObject target)
    {
        if (target == null)
            return;

        SpriteRenderer sprite = target.GetComponent<SpriteRenderer>();

        // Never overwrite an existing Visual sprite with an empty one.
        if (sprite == null || sprite.sprite == null)
            return;

        Transform visual = target.transform.Find("Visual");

        if (visual == null)
        {
            GameObject visualObject = new GameObject("Visual");
            visualObject.transform.SetParent(target.transform, false);
            visual = visualObject.transform;
        }

        SpriteRenderer moved = visual.GetComponent<SpriteRenderer>();

        if (moved == null)
            moved = visual.gameObject.AddComponent<SpriteRenderer>();

        moved.sprite = sprite.sprite;
        moved.color = sprite.color;
        moved.sortingOrder = sprite.sortingOrder;
        moved.flipX = sprite.flipX;
        moved.flipY = sprite.flipY;
        Object.DestroyImmediate(sprite);

        Billboard billboard = target.GetComponent<Billboard>();

        if (billboard != null)
        {
            SerializedObject serialized = new SerializedObject(billboard);
            serialized.FindProperty("visual").objectReferenceValue = visual;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(billboard);
        }

        SpriteFlipper flipper = target.GetComponent<SpriteFlipper>();

        if (flipper != null)
        {
            SerializedObject serialized = new SerializedObject(flipper);
            serialized.FindProperty("model").objectReferenceValue = visual;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(flipper);
        }
    }

    /// <summary>
    /// Projectiles historically carry a 0.18 root scale to shrink their
    /// sprite - which also shrinks every attached VFX to invisible size.
    /// Scale the root back to 1 and move the shrink into the Visual child.
    /// </summary>
    private static void NormalizeProjectileScale(GameObject target)
    {
        Transform root = target.transform;

        if (Mathf.Approximately(root.localScale.x, 1f))
            return;

        float shrink = root.localScale.x;
        Transform visual = root.Find("Visual");

        if (visual != null)
        {
            Vector3 visualScale = visual.localScale;
            visual.localScale = new Vector3(
                visualScale.x * shrink, visualScale.y * shrink, visualScale.z * shrink);
        }

        root.localScale = Vector3.one;
    }

    private static void ImportShadowSprite()
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(ShadowSpritePath);

        if (importer == null)
            throw new System.InvalidOperationException("Shadow sprite not found");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    private static void WidenCollider(GameObject target)
    {
        BoxCollider2D collider = target.GetComponent<BoxCollider2D>();

        if (collider != null)
        {
            // From the sprite's real bottom edge (bounds handle any pivot):
            // 10% inset from the bottom, 5% from each side - a low strip at
            // the base of the prop.
            SpriteRenderer sprite = target.GetComponentInChildren<SpriteRenderer>();

            if (sprite == null || sprite.sprite == null)
                return;

            Bounds bounds = sprite.sprite.bounds;
            float bottom = bounds.center.y - bounds.extents.y;

            collider.size = new Vector2(bounds.size.x * 0.9f, bounds.size.y * 0.15f);
            collider.offset = new Vector2(0f, bottom + bounds.size.y * 0.175f);
        }
    }

    private static void AddShadowToPrefab(string path, float size)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        AddShadow(contents, size);
        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static void AddShadowToPrefabs(string folder)
    {
        foreach (string file in Directory.GetFiles(folder, "*.prefab"))
            AddShadowToPrefab(file, 1.4f);
    }

    private static void RemoveShadowsFromProps(string folder)
    {
        foreach (string file in Directory.GetFiles(folder, "*.prefab"))
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(file);
            Transform shadow = contents.transform.Find("ShadowBlob");

            if (shadow != null)
                Object.DestroyImmediate(shadow.gameObject, true);

            PrefabUtility.SaveAsPrefabAsset(contents, file);
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static void ApplyEnemyTint(GameObject contents, string enemyName)
    {
        Color tint = enemyName switch
        {
            "Runner" => new Color(0.95f, 0.72f, 0.35f, 1f),
            "Brute" => new Color(0.92f, 0.42f, 0.42f, 1f),
            _ => new Color(0.55f, 0.85f, 0.48f, 1f)
        };

        Transform visual = contents.transform.Find("Visual");

        if (visual != null)
        {
            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();

            if (renderer != null)
                renderer.color = tint;
        }
    }

    private static void SetLayerForPrefabs(string folder, int layer)
    {
        if (layer < 0)
            return;

        foreach (string file in Directory.GetFiles(folder, "*.prefab"))
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(file);

            foreach (Transform child in contents.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = layer;

            PrefabUtility.SaveAsPrefabAsset(contents, file);
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }
}
