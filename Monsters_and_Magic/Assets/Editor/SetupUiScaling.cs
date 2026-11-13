using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Normalizes every screen-space canvas for cross-platform UI: proper
/// render mode, Scale With Screen Size, 1080x1920 portrait reference,
/// Expand match mode and safe-area anchors. Idempotent.
/// </summary>
public static class SetupUiScaling
{
    [MenuItem("Tools/Monsters and Magic/Setup UI Scaling")]
    public static void Setup()
    {
        foreach (string scene in new[] { "Assets/Scenes/Game.unity", "Assets/Scenes/MainMenu.unity" })
        {
            var previous = EditorSceneManager.GetSceneManagerSetup();
            bool loaded = EditorSceneManager.GetActiveScene().path == scene;

            if (!loaded)
                EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);

            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Normalize(canvas);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            if (!loaded && previous != null && previous.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(previous);

            Debug.Log("[UISetup] " + scene + " done");
        }

        // Canvas prefabs (HUD, end screen, health bars) too.
        foreach (string prefab in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" })
                     .Select(AssetDatabase.GUIDToAssetPath))
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(prefab);
            int changed = 0;

            foreach (Canvas canvas in contents.GetComponentsInChildren<Canvas>(true))
                changed += Normalize(canvas);

            if (changed > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(contents, prefab);
                Debug.Log("[UISetup] prefab " + prefab + " normalized");
            }

            PrefabUtility.UnloadPrefabContents(contents);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[UISetup] all canvases normalized.");
    }

    private static int Normalize(Canvas canvas)
    {
        if (canvas.renderMode == RenderMode.WorldSpace)
            return 0;

        int changed = 0;

        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            changed++;
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();

        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        else
            changed++;

        if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            changed++;
        }

        if (scaler.referenceResolution != new Vector2(1080f, 1920f))
        {
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            changed++;
        }

        // Match Width Or Height: portrait phones are height-dominant,
        // landscape desktop is width-dominant - Expand handles both.
        if (scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.Expand)
        {
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            changed++;
        }

        if (scaler.matchWidthOrHeight != 0.5f)
            scaler.matchWidthOrHeight = 0.5f;

        RectTransform rect = canvas.GetComponent<RectTransform>();

        if (rect.anchorMin != Vector2.zero || rect.anchorMax != Vector2.one)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            changed++;
        }

        return changed;
    }
}
