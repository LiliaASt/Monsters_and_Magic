using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

/// <summary>
/// Rebuilds combat VFX prefabs by borrowing particle setups from the Hovl Studio
/// "Magic effects pack" and converting every material to URP-compatible variants
/// (the pack ships with built-in pipeline shaders that render magenta under URP).
/// Lightning arcs use a minimal custom additive shader that also works on
/// LineRenderer meshes, which URP particle shaders refuse to render.
/// Existing prefab paths (and therefore GUIDs) are preserved, so all attack
/// and enemy configs keep their references.
/// </summary>
public static class CombatVfxSetup
{
    private const string CustomFolder = "Assets/Prefabs/VFX/Custom";
    private const string MaterialFolder = "Assets/Materials/VFX";
    private const string HovlMaterials = "Assets/Hovl Studio/Magic effects pack/Materials";

    // All VFX live in their own sorting layer above the whole world.
    private const string VfxSortingLayer = "VFX";
    private const int VfxSortingOrder = 20;

    public static void BuildFromCommandLine()
    {
        try
        {
            Build();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError("Combat VFX build failed:\n" + exception);
            EditorApplication.Exit(1);
        }
    }

    private static void EnsureSortingLayer()
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("m_SortingLayers");

        for (int i = 0; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue
                == VfxSortingLayer)
                return;
        }

        layers.InsertArrayElementAtIndex(layers.arraySize);
        SerializedProperty layer = layers.GetArrayElementAtIndex(layers.arraySize - 1);
        layer.FindPropertyRelative("name").stringValue = VfxSortingLayer;
        layer.FindPropertyRelative("uniqueID").intValue = 18651;
        tagManager.ApplyModifiedPropertiesWithoutUndo();
    }



    [MenuItem("Tools/Monsters and Magic/Rebuild Combat VFX")]
    public static void Build()
    {
        EnsureSortingLayer();
        EnsureFolder("Assets/Materials");
        EnsureFolder(MaterialFolder);
        EnsureFolder("Assets/Prefabs/VFX");
        EnsureFolder(CustomFolder);

        Material point = ConvertMaterial(LoadHovlMaterial("Point.mat"));
        Material glow = ConvertMaterial(LoadHovlMaterial("GlowFree1.mat"));
        Material circle = ConvertMaterial(LoadHovlMaterial("Circle.mat"));
        Material electro = ConvertMaterial(LoadHovlMaterial("Electro.mat"));
        Material smoke = ConvertMaterial(LoadHovlMaterial("SmokeFree1.mat"));
        Material flare = ConvertMaterial(LoadHovlMaterial("Flare.mat"));

        Material softSmoke = CreateSoftSmokeVariant();
        Material magicCircleBlue = CreateTintedCircleVariant();

        Shader additiveShader = LoadCustomAdditiveShader();
        Material missileTrail = CreateMissileTailMaterial(additiveShader);
        Material fireBlob = CreateUrpAdditiveMaterial(
            MaterialFolder + "/VFX_KenneyFire01_Additive.mat",
            LoadVfxTexture("Kenney/fire_01.png"),
            Color.white);
        Material fireOrbCore = CreateFireOrbMaterial(
            MaterialFolder + "/VFX_FireOrb_Core.mat", heatBias: 0f,
            noiseScale: 4.5f, noiseSpeed: 1.6f);
        Material fireOrbShell = CreateFireOrbMaterial(
            MaterialFolder + "/VFX_FireOrb_Shell.mat", heatBias: -0.28f,
            noiseScale: 3.4f, noiseSpeed: 1.15f);
        // Hovl electro cyan lives in particle startColors (~0.16, 0.56, 1.0);
        // for the arc lines we carry it in the material tint instead.
        Material lightningCore = CreateArcMaterial(
            MaterialFolder + "/VFX_LightningCore_Additive.mat",
            additiveShader, null, new Color(1.15f, 2.3f, 4.0f, 1f));
        Material lightningGlow = CreateArcMaterial(
            MaterialFolder + "/VFX_LightningGlow_Additive.mat",
            additiveShader,
            CreateLightningGlowTexture(),
            new Color(0.75f, 1.7f, 3.4f, 0.95f));
        Material lightningGlowOuter = CreateArcMaterial(
            MaterialFolder + "/VFX_LightningGlowOuter_Additive.mat",
            additiveShader,
            AssetDatabase.LoadAssetAtPath<Texture>("Assets/Textures/VFX/Custom/LightningGlow.png"),
            new Color(0.55f, 1.35f, 2.9f, 0.65f));

        GameObject missileFlight = BuildMissileFlight(point, missileTrail);
        GameObject missileHit = BuildMissileHit(point, glow);
        GameObject fireballFlight = BuildFireballFlight(
            point, smoke, fireBlob, fireOrbCore, fireOrbShell);
        GameObject fireballHit = BuildFireballHit(softSmoke);
        GameObject lightningHit = BuildLightningHit();
        GameObject lightningArc = BuildLightningArc(lightningCore, lightningGlow, lightningGlowOuter);
        GameObject novaBurst = BuildNovaBurst(glow, circle, magicCircleBlue);
        GameObject enemyDeath = BuildEnemyDeath(point, glow);
        GameObject meleeHit = BuildMeleeHit(glow);
        GameObject portalSpawn = BuildPortalSpawn();
        GameObject levelUp = BuildLevelUpEffect(point);

        ConfigureFireballProjectile();

        ConfigureGlobalVolume();
        CleanupLegacyAssets();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Combat VFX rebuilt: Hovl effects borrowed, materials converted to URP, VfxEffect attached.");
    }

    // ------------------------------------------------------------------ flight
    // Flight effects follow the "one projectile, one glow" rule: a single
    // steady glow quad (burst of one particle) plus distance-based trails.
    // Stacked rapidly-emitted glow quads read as "many balls".

    private static GameObject BuildMissileFlight(Material point, Material cometTrail)
    {
        GameObject root = new GameObject("MagicMissileFlight");

        // The head IS the Snow AOE spark look: a continuous cluster of flying
        // stretched sparks (their exact renderer settings), plus a thin trail.
        BuildSnowSparks(root.transform, "Sparks", point,
            new Color(0.9f, 0.55f, 1.25f, 1f),
            minSize: 0.035f, maxSize: 0.06f,
            minSpeed: 2f, maxSpeed: 3.5f,
            minLife: 0.15f, maxLife: 0.25f, rate: 32f);

        BuildSparkTrail(root.transform, "Trail", point,
            new Color(0.8f, 0.5f, 1.15f, 1f),
            rateOverDistance: 11f, lifetime: 0.45f, size: 0.08f);

        // Comet tail: Hovl's Trail67 is a purpose-made streak texture
        // (bright head fading out along its length). A TrailRenderer drags
        // it along the projectile's actual path, so the tail reads as one
        // continuous light ribbon instead of sparse particles. The child
        // object + WorldSpaceTrail counter-rotation lets the ribbon bend
        // when the projectile steers instead of sticking out straight.
        GameObject trailObject = new GameObject("CometTrail");
        trailObject.transform.SetParent(root.transform, false);
        TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
        trailObject.AddComponent<WorldSpaceTrail>();
        trail.time = 0.3f;
        trail.minVertexDistance = 0.05f;
        trail.widthCurve = AnimationCurve.EaseInOut(0f, 0.18f, 1f, 0.015f);
        trail.numCapVertices = 2;
        trail.numCornerVertices = 2;
        trail.textureMode = LineTextureMode.Stretch;
        trail.sharedMaterial = cometTrail;
        trail.sortingOrder = VfxSortingOrder - 1;

        ConfigureParticles(root, looping: true);
        AddVfxEffect(root, loop: true);
        return SavePrefab(root, CustomFolder + "/MagicMissileFlight.prefab");
    }

    private static GameObject BuildFireballFlight(Material point, Material smoke, Material fireBlob,
        Material fireOrbCore, Material fireOrbShell)
    {
        GameObject root = new GameObject("FireballFlight");

        // The ball is a real shader-driven sphere pair: a hot core orb plus
        // a larger, cooler shell (negative heat bias) that reads as a red
        // burning halo around it - that layering is what makes it feel
        // volumetric instead of a flat ball. Everything else is exhaust:
        // flame trail, embers and warm smoke behind the sphere.
        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "FireOrbCore";
        Object.DestroyImmediate(core.GetComponent<Collider>());
        core.transform.SetParent(root.transform, false);
        core.transform.localScale = new Vector3(1.25f, 1.25f, 1.25f);

        MeshRenderer coreRenderer = core.GetComponent<MeshRenderer>();
        coreRenderer.sharedMaterial = fireOrbCore;
        coreRenderer.sortingLayerName = VfxSortingLayer;
        coreRenderer.sortingOrder = VfxSortingOrder;

        GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shell.name = "FireOrbShell";
        Object.DestroyImmediate(shell.GetComponent<Collider>());
        shell.transform.SetParent(root.transform, false);
        shell.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);

        MeshRenderer shellRenderer = shell.GetComponent<MeshRenderer>();
        shellRenderer.sharedMaterial = fireOrbShell;
        shellRenderer.sortingLayerName = VfxSortingLayer;
        shellRenderer.sortingOrder = VfxSortingOrder - 1;

        // Burning tail, no ribbons: a dense stream of fire blobs left behind
        // in world space. Each blob flares up, drifts upward like real flame
        // and dies down to embers - that churn is what reads as combustion,
        // while any trail ribbon reads as a strap or a triangle.
        BuildBurningTrail(root.transform, "FireTrail", fireBlob);

        BuildSnowSparks(root.transform, "Embers", point,
            new Color(1.0f, 0.55f, 0.2f, 1f),
            minSize: 0.06f, maxSize: 0.11f,
            minSpeed: 1.5f, maxSpeed: 3f,
            minLife: 0.15f, maxLife: 0.25f, rate: 7f);

        BuildSmokeTrail(root.transform, "SmokeTrail", smoke,
            new Color(0.35f, 0.27f, 0.22f, 1f),
            rateOverDistance: 8f, lifetime: 0.9f, size: 0.32f);

        ConfigureParticles(root, looping: true);

        // ConfigureParticles resets every system to the shared order (20),
        // which sinks the trail below Y-sorted world sprites (~1000). The
        // burning trail must draw above characters and props.
        ParticleSystemRenderer trailRenderer =
            root.transform.Find("FireTrail").GetComponent<ParticleSystemRenderer>();
        trailRenderer.sortingLayerName = VfxSortingLayer;
        trailRenderer.sortingOrder = 19;

        AddVfxEffect(root, loop: true);
        return SavePrefab(root, CustomFolder + "/FireballFlight.prefab");
    }

    // ------------------------------------------------------------------ hits

    private static GameObject BuildMissileHit(Material point, Material glow)
    {
        GameObject root = new GameObject("MagicMissileHit");

        // A small controlled spark burst instead of the Hovl spark shower.
        BuildSparkBurst(root.transform, "Sparks", point,
            new Color(0.8f, 0.5f, 1.55f, 1f),
            count: 9, minSpeed: 1.2f, maxSpeed: 2.2f,
            minSize: 0.05f, maxSize: 0.09f);

        BuildBurst(root.transform, "Flash", glow,
            new Color(1.0f, 0.6f, 1.8f, 1f),
            lifetime: 0.15f, startSize: 0.5f, endSize: 0.15f);

        ConfigureParticles(root, looping: false);
        AddVfxEffect(root, loop: false);
        return SavePrefab(root, CustomFolder + "/MagicMissileHit.prefab");
    }

    private static GameObject BuildFireballHit(Material softSmoke)
    {
        GameObject root = new GameObject("FireballHit");

        // Hovl Explosion: flash + shockwave + sparks + smoke, with the smoke
        // swapped to a much more transparent variant.
        GameObject explosion = AddHovlPart(root.transform, "Hits and explosions/Explosion", 0.4f);
        SwapChildMaterial(explosion, "Smoke", softSmoke);

        ConfigureParticles(root, looping: false);
        AddVfxEffect(root, loop: false);
        return SavePrefab(root, CustomFolder + "/FireballHit.prefab");
    }

    private static GameObject BuildLightningHit()
    {
        GameObject root = new GameObject("LightningHit");

        // The ready-made Hovl aura untouched: only materials get converted
        // to URP; gravity, rotation and colors stay as the artist made them -
        // that is what keeps it an electric aura instead of a fog blob.
        GameObject hit = AddHovlPart(root.transform,
            "Character auras/Lightning aura", 0.4f);

        // A quick, THIN and DIM 0.4s flash.
        foreach (ParticleSystem particles in hit.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.4f;
            main.startLifetime = 0.4f;
            main.startSize = main.startSize.constantMax * 0.65f;

            Color original = main.startColor.colorMax;
            main.startColor = new Color(
                original.r * 0.6f, original.g * 0.6f, original.b * 0.6f,
                original.a * 0.75f);
        }

        ConfigureParticlesMinimal(root);
        AddVfxEffect(root, loop: false);
        return SavePrefab(root, CustomFolder + "/LightningHit.prefab");
    }

    private static GameObject BuildMeleeHit(Material glow)
    {
        GameObject root = new GameObject("MeleeHit");

        AddHovlPart(root.transform, "Sparks/Sparks explode white", 0.28f);

        BuildBurst(root.transform, "Flash", glow,
            new Color(1.6f, 1.5f, 1.2f, 1f),
            lifetime: 0.12f, startSize: 0.35f, endSize: 0.1f);

        ConfigureParticles(root, looping: false);
        AddVfxEffect(root, loop: false);
        return SavePrefab(root, CustomFolder + "/MeleeHit.prefab");
    }

    private static GameObject BuildLevelUpEffect(Material point)
    {
        GameObject root = new GameObject("LevelUpEffect");

        // The original Hovl Love aura with hearts and smoke removed: its
        // native swirl rises beautifully. We only recolor the remaining
        // trail particles to soft white-gold and set duration.
        GameObject aura = AddHovlPart(root.transform, "Character auras/Love aura", 1.0f);

        // Hearts live on the root object, smoke is a child - cut both.
        ParticleSystem hearts = aura.GetComponent<ParticleSystem>();
        if (hearts != null)
            Object.DestroyImmediate(hearts);

        ParticleSystemRenderer heartsRenderer = aura.GetComponent<ParticleSystemRenderer>();
        if (heartsRenderer != null)
            Object.DestroyImmediate(heartsRenderer);

        RemoveChild(aura, "Smoke");

        // Keep only the trail particles; recolor them to soft white-gold
        // (not HDR - the trails carry the brightness naturally).
        foreach (ParticleSystem particles in aura.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = particles.main;
            main.duration = 1.1f;
            main.startLifetime = 1.0f;
            main.startColor = new Color(1.3f, 1.15f, 0.7f, 1f);
        }

        // Our dedicated soft-gold trail material on the URP particle shader.
        ConfigureParticlesMinimal(root);

        // Gold trail AFTER material conversion: ConfigureParticlesMinimal
        // rewrites renderers to converted Hovl materials (with the shared
        // cyan tint), so the gold override must come last.
        Material goldTrail = CreateUrpAdditiveMaterial(
            MaterialFolder + "/VFX_LevelUpTrail_Additive.mat",
            AssetDatabase.LoadAssetAtPath<Texture>(
                "Assets/Hovl Studio/Magic effects pack/Textures/Trail67.png"),
            new Color(1.3f, 1.15f, 0.65f, 0.95f));

        foreach (Renderer effectRenderer in root.GetComponentsInChildren<Renderer>(true))
            effectRenderer.sharedMaterial = goldTrail;
        AddVfxEffect(root, loop: false);
        return SavePrefab(root, CustomFolder + "/LevelUpEffect.prefab");
    }

    // ------------------------------------------------------------------ nova / death / portal

    private static GameObject BuildNovaBurst(Material glow, Material circle, Material magicCircleBlue)
    {
        GameObject root = new GameObject("NovaBurst");

        // Hovl runic magic circle: blue-tinted, one-shot, without 3D side walls.
        GameObject runic = AddHovlPart(root.transform, "Magic circles/Magic circle", 1.0f);
        RemoveChild(runic, "Sides");
        // Keep the sparkle children for the rich look, but trim their
        // lifetime so nothing dances after the nova has dealt its damage.
        TrimChildLifetime(runic, "Sparks", 1.1f);
        TrimChildLifetime(runic, "Light", 1.1f);
        TrimChildLifetime(runic, "CircleSparks", 1.1f);
        TrimChildLifetime(runic, "DarkSparks", 1.1f);
        SwapChildMaterial(runic, "Magic circle", magicCircleBlue);
        TrimChildLifetime(runic, "Magic circle", 1.1f);

        BuildBurst(root.transform, "Ring", circle,
            new Color(0.55f, 0.9f, 1.8f, 1f),
            lifetime: 0.45f, startSize: 0.4f, endSize: 2.6f,
            randomRotation: true);

        BuildBurst(root.transform, "RingOuter", circle,
            new Color(0.35f, 0.6f, 1.2f, 0.7f),
            lifetime: 0.55f, startSize: 0.25f, endSize: 3.1f,
            randomRotation: true, delay: 0.04f);

        // No lingering post-effect: the rune, rings and flash are short and
        // snappy, and the old rising motes kept dancing long after the nova
        // had dealt its damage.

        BuildBurst(root.transform, "Flash", glow,
            new Color(1.2f, 1.6f, 2.2f, 1f),
            lifetime: 0.2f, startSize: 1.0f, endSize: 0.25f);

        ConfigureParticles(root, looping: false);
        AddVfxEffect(root, loop: false);
        return SavePrefab(root, "Assets/Prefabs/VFX/NovaBurst.prefab");
    }

    

    private static GameObject BuildEnemyDeath(Material point, Material glow)
    {
        GameObject root = new GameObject("EnemyDeathSmall");

        // Magical dissipation: violet wisps drift up and fade away.
        // No smoke, no explosion - monsters dissolve into magic.
        BuildMagicDissipation(root.transform, "Wisps", point,
            new Color(0.75f, 0.5f, 1.4f, 1f));

        BuildBurst(root.transform, "Glow", glow,
            new Color(1.1f, 0.85f, 1.8f, 0.7f),
            lifetime: 0.26f, startSize: 0.5f, endSize: 0.1f);

        ConfigureParticles(root, looping: false);
        AddVfxEffect(root, loop: false);
        return SavePrefab(root, CustomFolder + "/EnemyDeathSmall.prefab");
    }

    private static GameObject BuildPortalSpawn()
    {
        GameObject root = new GameObject("PortalSpawn");

        GameObject portal = AddHovlPart(root.transform, "Portals/Portal blue", 1.1f);

        // Distortion relies on a grab pass that does not exist under the 2D URP renderer.
        Transform distortion = portal.transform.Find("Distortion");
        if (distortion != null)
            Object.DestroyImmediate(distortion.gameObject);

        // Portal stays looping: EnemySpawner destroys it after portalDelay.
        ConfigureParticles(root, looping: true);
        AddVfxEffect(root, loop: true);
        return SavePrefab(root, CustomFolder + "/PortalSpawn.prefab");
    }

    // ------------------------------------------------------------------ lightning arc

    private static GameObject BuildLightningArc(Material core, Material glow, Material glowOuter)
    {
        GameObject root = new GameObject("LightningArc");
        // Thin discharge core: barely wider than a hair and saturated cyan -
        // a white fat core reads as a pale rope, not electricity.
        ConfigureArcLine(root.AddComponent<LineRenderer>(), core,
            0.012f, 0.005f, 22);

        GameObject glowLine = new GameObject("Glow");
        glowLine.transform.SetParent(root.transform, false);
        ConfigureArcLine(glowLine.AddComponent<LineRenderer>(), glow,
            0.15f, 0.06f, 21);

        // Wide dissolving halo around the arc - with the tight glow this is
        // the two-layer plasma look (hot core, inner sheath, outer halo).
        GameObject outerLine = new GameObject("GlowOuter");
        outerLine.transform.SetParent(root.transform, false);
        ConfigureArcLine(outerLine.AddComponent<LineRenderer>(), glowOuter,
            0.28f, 0.12f, 20);

        // A single branch fork (core + glow). LightningArcAnimator positions
        // and re-randomizes it together with the main arc every flicker.
        for (int i = 0; i < 1; i++)
        {
            GameObject branchCore = new GameObject("BranchCore" + i);
            branchCore.transform.SetParent(root.transform, false);
            ConfigureArcLine(branchCore.AddComponent<LineRenderer>(), core,
                0.011f, 0.005f, 30);

            GameObject branchGlow = new GameObject("BranchGlow" + i);
            branchGlow.transform.SetParent(root.transform, false);
            ConfigureArcLine(branchGlow.AddComponent<LineRenderer>(), glow,
                0.09f, 0.04f, 29);
        }

        root.AddComponent<LightningArcAnimator>();
        AddVfxEffect(root, loop: false);

        return SavePrefab(root, CustomFolder + "/LightningArc.prefab");
    }

    private static void ConfigureArcLine(LineRenderer line, Material material,
        float startWidth, float endWidth, int sortingOrder)
    {
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.numCapVertices = 2;
        line.textureMode = LineTextureMode.Tile;
        line.textureScale = Vector2.one;
        line.generateLightingData = true;
        line.sortingOrder = sortingOrder;
        line.startWidth = startWidth;
        line.endWidth = endWidth;
        line.startColor = Color.white;
        line.endColor = new Color(1f, 1f, 1f, 0.7f);
        line.sharedMaterial = material;
    }

    // ------------------------------------------------------------------ global volume

    /// <summary>
    /// Normal post-processing for the gameplay scene: bloom makes the additive
    /// VFX glow, a light vignette focuses the arena, and slight contrast and
    /// saturation make the palette pop.
    /// </summary>
    private static void ConfigureGlobalVolume()
    {
        const string path = "Assets/Scenes/Game/Global Volume Profile 1.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);

        if (profile == null)
            throw new InvalidOperationException("Global Volume profile not found: " + path);

        Bloom bloom;
        if (!profile.TryGet(out bloom))
        {
            bloom = profile.Add<Bloom>(true);
            AssetDatabase.AddObjectToAsset(bloom, profile);
        }

        bloom.active = true;
        bloom.threshold.Override(0.92f);
        bloom.intensity.Override(1.45f);
        bloom.scatter.Override(0.72f);
        bloom.clamp.Override(65000f);
        bloom.tint.Override(Color.white);
        bloom.highQualityFiltering.Override(true);

        Vignette vignette;
        if (!profile.TryGet(out vignette))
        {
            vignette = profile.Add<Vignette>(true);
            AssetDatabase.AddObjectToAsset(vignette, profile);
        }

        vignette.active = true;
        vignette.intensity.Override(0.24f);
        vignette.smoothness.Override(0.45f);
        vignette.rounded.Override(false);

        ColorAdjustments color;
        if (!profile.TryGet(out color))
        {
            color = profile.Add<ColorAdjustments>(true);
            AssetDatabase.AddObjectToAsset(color, profile);
        }

        color.active = true;
        color.postExposure.Override(0f);
        color.contrast.Override(8f);
        color.saturation.Override(10f);

        EditorUtility.SetDirty(profile);
    }

    // ------------------------------------------------------------------ custom particle builders

    /// <summary>
    /// One persistent glow quad: a single burst particle with a long lifetime.
    /// No emission stacking, so the projectile always reads as one ball.
    /// </summary>
    private static void BuildSteadyGlow(Transform parent, string name, Material material,
        Color color, float size, float rotationSpeed = 0f)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.prewarm = false;
        main.duration = 10f;
        main.startLifetime = 10f;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startColor = color;
        main.maxParticles = 4;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        if (rotationSpeed != 0f)
        {
            ParticleSystem.RotationOverLifetimeModule rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(rotationSpeed * Mathf.Deg2Rad);
        }

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }
    /// <summary>
    /// Hides the projectile's placeholder SpriteRenderer - the fireball body
    /// is a shader-driven sphere inside the flight effect, not a sprite.
    /// </summary>
    private static void ConfigureFireballProjectile()
    {
        const string prefabPath = "Assets/Prefabs/Combat/FireballProjectile.prefab";

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        SpriteRenderer renderer = prefab != null ? prefab.GetComponent<SpriteRenderer>() : null;

        if (renderer == null)
            throw new InvalidOperationException("FireballProjectile sprite renderer is missing");

        renderer.enabled = false;
        PrefabUtility.SavePrefabAsset(prefab);
        Debug.Log("FireballProjectile sprite hidden - the fire orb sphere drives the visuals.");
    }

    /// <summary>
    /// Burning tail: a dense stream of fire blobs left behind in world space.
    /// Each blob flares up, drifts upward like real flame, spins slowly and
    /// dies down to deep red - churn instead of a geometric ribbon.
    /// </summary>
    private static void BuildBurningTrail(Transform parent, string name, Material material)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.prewarm = true;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.9f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(1.3f, 1.9f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = Color.white;
        main.maxParticles = 256;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 32f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.15f;
        shape.position = Vector3.zero;
        shape.rotation = Vector3.zero;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        // All three axes must share one curve mode (two constants).
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalY = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0f, 0f);


        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.45f),
            new Keyframe(0.3f, 1f),
            new Keyframe(1f, 0.12f)));

        ParticleSystem.RotationOverLifetimeModule rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(50f * Mathf.Deg2Rad);

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(2.8f, 1.4f, 0.5f), 0f),
                new GradientColorKey(new Color(1.5f, 0.45f, 0.1f), 0.55f),
                new GradientColorKey(new Color(0.45f, 0.08f, 0.02f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.12f),
                new GradientAlphaKey(0.8f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 25;
    }

    /// <summary>
    /// Continuous Snow-AOE-style spark cluster: short-lived Point-texture
    /// sparks flying outward, each stretched along its own velocity exactly
    /// like the Sparks system of Hovl's Snow AOE (velocityScale 0.05,
    /// lengthScale 2, size 0.05-0.1, speed 3-7).
    /// </summary>
    private static void BuildSnowSparks(Transform parent, string name, Material material,
        Color color, float minSize, float maxSize, float minSpeed, float maxSpeed,
        float minLife, float maxLife, float rate)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.prewarm = true;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = color;
        main.maxParticles = 256;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = rate;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.03f;


        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ApplyAlphaFade(ps, color, 1f, 0f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.05f;
        renderer.lengthScale = 2f;
    }

    /// <summary>
    /// Small controlled spark burst for hit feedback.
    /// </summary>
    private static void BuildSparkBurst(Transform parent, string name, Material material,
        Color color, int count, float minSpeed, float maxSpeed, float minSize, float maxSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.playOnAwake = true;
        main.prewarm = false;
        main.duration = 0.6f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = color;
        main.maxParticles = 32;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;


        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ApplyAlphaFade(ps, color, 1f, 0f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 1.5f;
    }

    

    private static void RemoveChild(GameObject root, string name)
    {
        Transform child = root.transform.Find(name);

        if (child != null)
            Object.DestroyImmediate(child.gameObject);
    }

    private static void SwapChildMaterial(GameObject root, string childName, Material material)
    {
        // The target may be the borrowed prefab's root itself, which
        // Transform.Find does not match.
        Transform child = root.name == childName
            ? root.transform
            : root.transform.Find(childName);

        if (child == null)
            return;

        ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();

        if (renderer != null)
            renderer.sharedMaterial = material;
    }

    private static void TrimChildLifetime(GameObject root, string childName, float lifetime)
    {
        Transform child = root.name == childName
            ? root.transform
            : root.transform.Find(childName);

        if (child == null)
            return;

        // Trim every particle system inside the child (Hovl parts nest).
        foreach (ParticleSystem particle in child.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = particle.main;
            main.startLifetime = lifetime;
            main.duration = Mathf.Min(main.duration, lifetime);
        }
    }

    /// <summary>
    /// Magical death dissipation: a burst of small glowing wisps that spread
    /// out from the body, drift gently upward and fade away.
    /// </summary>
    private static void BuildMagicDissipation(Transform parent, string name, Material material,
        Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.playOnAwake = true;
        main.prewarm = false;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.7f, 1.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.15f);
        main.startColor = color;
        main.maxParticles = 32;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.22f;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(0.35f);


        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ApplyAlphaFade(ps, color, 1f, 0f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private static void BuildSparkTrail(Transform parent, string name, Material material,
        Color color, float rateOverDistance, float lifetime, float size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.prewarm = false;
        main.duration = 1f;
        main.startLifetime = lifetime;
        main.startSpeed = 0.15f;
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.7f, size);
        main.startColor = color;
        main.maxParticles = 512;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = rateOverDistance;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;


        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ApplyAlphaFade(ps, color, 1f, 0f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.08f;
        renderer.lengthScale = 1.4f;
    }

    private static void BuildSmokeTrail(Transform parent, string name, Material material,
        Color color, float rateOverDistance, float lifetime, float size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.prewarm = false;
        main.duration = 1f;
        main.startLifetime = lifetime;
        main.startSpeed = 0.1f;
        main.startSize = size;
        main.startColor = color;
        main.maxParticles = 256;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = rateOverDistance;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.08f;


        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f, AnimationCurve.EaseInOut(0f, 0.3f, 1f, 1.6f));

        ApplyAlphaFade(ps, color, 0.4f, 0f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private static void BuildBurst(Transform parent, string name, Material material,
        Color color, float lifetime, float startSize, float endSize,
        bool randomRotation = false, float delay = 0f)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.playOnAwake = true;
        main.prewarm = false;
        main.duration = lifetime + delay + 0.05f;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = 1f;
        main.startColor = Color.white;
        main.maxParticles = 16;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        if (randomRotation)
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(delay, 1) });


        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f, AnimationCurve.EaseInOut(0f, startSize, 1f, endSize));

        ApplyAlphaFade(ps, color, color.a, 0f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private static void ApplyAlphaFade(ParticleSystem ps, Color color, float alphaFrom, float alphaTo)
    {
        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            new[]
            {
                new GradientAlphaKey(alphaFrom, 0f),
                new GradientAlphaKey(Mathf.Lerp(alphaFrom, alphaTo, 0.7f), 0.6f),
                new GradientAlphaKey(alphaTo, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
    }

    // ------------------------------------------------------------------ borrowed Hovl parts

    private static GameObject AddHovlPart(Transform parent, string relativePath, float scale)
    {
        string suffix = "Hovl Studio/Magic effects pack/Prefabs/" + relativePath + ".prefab";
        GameObject source = FindHovlPrefab(suffix);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one * scale;
        return instance;
    }

    private static GameObject FindHovlPrefab(string suffix)
    {
        string filename = Path.GetFileNameWithoutExtension(suffix);

        foreach (string guid in AssetDatabase.FindAssets(filename))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        throw new InvalidOperationException("Hovl prefab not found: " + suffix);
    }

    /// <summary>
    /// Applies top-down 2D adjustments to every borrowed particle system:
    /// no gravity, no billboard-flipping X/Y rotation, URP-converted
    /// materials and a sorting order above gameplay sprites.
    /// LimitVelocityOverLifetime is intentionally left untouched: Hovl uses
    /// it as artistic drag (dampen 0.1-0.3), and with an orthographic
    /// top-down camera particle Z positions are invisible.
    /// </summary>

    /// <summary>
    /// Only what is strictly required for a Hovl part under URP: convert
    /// materials and stop looping so VfxEffect can reap the object. Gravity,
    /// rotation and particle colors stay exactly as the artist made them.
    /// </summary>
    private static void ConfigureParticlesMinimal(GameObject root)
    {
        foreach (ParticleSystem ps in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
        }

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sortingLayerName = VfxSortingLayer;
            renderer.sortingOrder = VfxSortingOrder;
            ConvertRendererMaterials(renderer);
        }
    }

    private static void ConfigureParticles(GameObject root, bool looping)
    {
        foreach (ParticleSystem ps in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = ps.main;
            main.loop = looping;
            main.gravityModifier = 0f;

            if (main.startRotation3D)
            {
                main.startRotationX = new ParticleSystem.MinMaxCurve(0f);
                main.startRotationY = new ParticleSystem.MinMaxCurve(0f);
            }

            ParticleSystem.RotationOverLifetimeModule rotation = ps.rotationOverLifetime;
            if (rotation.enabled && rotation.separateAxes)
            {
                rotation.x = new ParticleSystem.MinMaxCurve(0f);
                rotation.y = new ParticleSystem.MinMaxCurve(0f);
            }
        }

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sortingLayerName = VfxSortingLayer;
            renderer.sortingOrder = VfxSortingOrder;
            ConvertRendererMaterials(renderer);
        }
    }

    // ------------------------------------------------------------------ materials

    private static Material LoadHovlMaterial(string name)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(HovlMaterials + "/" + name);

        if (material == null)
            throw new InvalidOperationException("Hovl material not found: " + name);

        return material;
    }

    private static void ConvertRendererMaterials(Renderer renderer)
    {
        Material[] shared = renderer.sharedMaterials;
        bool changed = false;

        for (int i = 0; i < shared.Length; i++)
        {
            Material converted = ConvertMaterial(shared[i]);

            if (!ReferenceEquals(converted, shared[i]))
            {
                shared[i] = converted;
                changed = true;
            }
        }

        if (changed)
            renderer.sharedMaterials = shared;
    }

    private static Material ConvertMaterial(Material source)
    {
        if (source == null)
            return null;

        if (source.shader.name.StartsWith("Universal Render Pipeline", StringComparison.Ordinal)
            || source.shader.name.StartsWith("Monsters and Magic/", StringComparison.Ordinal))
            return source;

        bool additive = !source.name.Contains("Smoke", StringComparison.OrdinalIgnoreCase)
                      && !source.name.Contains("Distortion", StringComparison.OrdinalIgnoreCase);

        string path = MaterialFolder + "/VFX_" + SanitizeName(source.name)
                    + (additive ? "_Additive" : "_Alpha") + ".mat";

        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(LoadUrpParticlesShader());
            AssetDatabase.CreateAsset(material, path);
        }

        Texture texture = source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null;
        Color color = source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", color);
        // URP keeps a serialized _Color alias that overrides visual output
        // in some shaders - keep it in sync to avoid stale tint.
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        ConfigureTransparent(material, additive);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateSoftSmokeVariant()
    {
        Material source = LoadHovlMaterial("Smoke26.mat");
        string path = MaterialFolder + "/VFX_Smoke26_Soft_Alpha.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(LoadUrpParticlesShader());
            AssetDatabase.CreateAsset(material, path);
        }

        Color color = source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
        color.a = 0.45f;

        material.SetTexture("_BaseMap", source.GetTexture("_MainTex"));
        material.SetColor("_BaseColor", color);
        ConfigureTransparent(material, additive: false);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateTintedCircleVariant()
    {
        Material source = LoadHovlMaterial("MagicCircle.mat");
        string path = MaterialFolder + "/VFX_MagicCircle_Blue_Additive.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(LoadUrpParticlesShader());
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetTexture("_BaseMap", source.GetTexture("_MainTex"));
        material.SetColor("_BaseColor", new Color(0.55f, 0.85f, 1.7f, 1f));
        ConfigureTransparent(material, additive: true);
        EditorUtility.SetDirty(material);
        return material;
    }

    /// <summary>
    /// Additive material for the CC0 fireball flipbook (Cethiel, OpenGameArt).
    /// The source frames already have proper alpha, so they composite cleanly
    /// under additive blending and feed the bloom pass.
    /// </summary>
    private static Shader LoadUrpParticlesShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        if (shader == null)
            throw new InvalidOperationException("URP Particles/Unlit shader not found.");

        return shader;
    }

    private static Texture LoadHovlTexture(string name)
    {
        Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(
            "Assets/Hovl Studio/Magic effects pack/Textures/" + name);

        if (texture == null)
            throw new InvalidOperationException("Hovl texture not found: " + name);

        return texture;
    }

    /// <summary>
    /// Missile tail material on a generated soft-streak texture: gaussian
    /// cross-section (edges dissolve to zero alpha), exponential falloff
    /// towards the tail and a brighter core line. Trail67 has hard edge
    /// alpha, which reads as a sharp triangle under additive blending.
    /// </summary>
    private static Material CreateMissileTailMaterial(Shader shader)
    {
        const string texturePath = "Assets/Textures/VFX/Custom/MissileTail.png";

        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("Missile tail texture not found: " + texturePath);

        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();

        return CreateArcMaterial(
            MaterialFolder + "/VFX_MissileTail_Additive.mat",
            shader,
            AssetDatabase.LoadAssetAtPath<Texture>(texturePath),
            new Color(1.15f, 0.55f, 1.65f, 0.9f));
    }

    /// <summary>
    /// Soft glow texture for the lightning arc lines: gaussian falloff
    /// across the beam (edges dissolve instead of cutting off) plus a
    /// seamless shimmer along it, same language as the missile tail.
    /// </summary>
    private static Texture CreateLightningGlowTexture()
    {
        const string texturePath = "Assets/Textures/VFX/Custom/LightningGlow.png";

        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("Lightning glow texture not found: " + texturePath);

        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
    }


    private static Texture LoadVfxTexture(string relativePath)
    {
        Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(
            "Assets/Textures/VFX/" + relativePath);

        if (texture == null)
            throw new InvalidOperationException("VFX texture not found: " + relativePath);

        return texture;
    }

    private static Shader LoadCustomAdditiveShader()
    {
        Shader shader = Shader.Find("Monsters and Magic/VFX/Additive Unlit");

        if (shader == null)
            throw new InvalidOperationException("Custom additive shader not found.");

        return shader;
    }

    private static Material CreateArcMaterial(string path, Shader shader, Texture texture, Color tint)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.SetTexture("_MainTex", texture);
        material.SetColor("_TintColor", tint);
        EditorUtility.SetDirty(material);
        return material;
    }

    /// <summary>
    /// Standard URP particle material for billboard particle systems. Unlike
    /// the line-renderer additive shader, it reads particle vertex color, so
    /// ColorOverLifetime gradients and alpha fades actually reach the GPU -
    /// without it fire reads as a few pale texture crops.
    /// </summary>
    private static Material CreateUrpAdditiveMaterial(string path, Texture texture, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(LoadUrpParticlesShader());
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = LoadUrpParticlesShader();
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", color);
        ConfigureTransparent(material, additive: true);
        EditorUtility.SetDirty(material);
        return material;
    }

    /// <summary>
    /// Fire orb material on the hand-written URP shader (the code equivalent
    /// of a Shader Graph): animated domain-warped flame noise over a sphere,
    /// HDR fire gradient and a soft additive rim.
    /// </summary>
    private static Material CreateFireOrbMaterial(string path, float heatBias,
        float noiseScale, float noiseSpeed)
    {
        Shader shader = Shader.Find("Monsters and Magic/VFX/Fire Orb");
        if (shader == null)
            throw new InvalidOperationException("Fire Orb shader not found");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.SetColor("_HotColor", new Color(3.4f, 2.0f, 0.62f, 1f));
        material.SetColor("_MidColor", new Color(2.2f, 0.78f, 0.15f, 1f));
        material.SetColor("_EdgeColor", new Color(0.88f, 0.12f, 0.02f, 1f));
        material.SetFloat("_NoiseScale", noiseScale);
        material.SetFloat("_NoiseSpeed", noiseSpeed);
        material.SetFloat("_WarpStrength", 1.4f);
        material.SetFloat("_HeatBias", heatBias);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigureTransparent(Material material, bool additive)
    {
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", additive ? 2f : 0f);
        material.SetFloat("_Cull", 0f);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_DstBlendAlpha", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.DisableKeyword("_ALPHAMODULATE_ON");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
    }

    private static string SanitizeName(string name)
    {
        return string.Concat(name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries))
                    .Replace(' ', '_');
    }

    // ------------------------------------------------------------------ vfx lifecycle

    /// <summary>
    /// Attaches the single runtime VFX driver. Loop marks effects that live
    /// until their owner dies (projectile trails, portals); everything else
    /// self-destructs once its particles finish.
    /// </summary>
    private static void AddVfxEffect(GameObject root, bool loop)
    {
        VfxEffect vfx = root.GetComponent<VfxEffect>();
        if (vfx == null)
            vfx = root.AddComponent<VfxEffect>();
        vfx.loop = loop;
    }

    // ------------------------------------------------------------------ misc

    private static void CleanupLegacyAssets()
    {
        // Orphaned duplicates from the previous setup; only Custom/ prefabs and
        // NovaBurst (referenced by Hero1_Nova) stay alive.
        string[] legacy =
        {
            "Assets/Prefabs/VFX/FireballHit.prefab",
            "Assets/Prefabs/VFX/FireballTrail.prefab",
            "Assets/Prefabs/VFX/MissileHit.prefab",
            "Assets/Prefabs/VFX/MissileTrail.prefab",
            "Assets/Prefabs/VFX/PortalBlue.prefab",
            "Assets/Prefabs/VFX/Custom/LightningArc.mat",
            "Assets/Materials/VFX/VFX_Electro_Glow_Additive.mat",
            "Assets/Materials/VFX/VFX_Gradient_Additive.mat",
            "Assets/Materials/VFX/VFX_Snowflake_Additive.mat",
            "Assets/Prefabs/VFX/Custom/LightningCrackle.prefab"
        };

        foreach (string path in legacy)
        {
            if (File.Exists(path))
                AssetDatabase.DeleteAsset(path);
        }
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
