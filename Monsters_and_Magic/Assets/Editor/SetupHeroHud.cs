using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Places the hero XP bar at the top of the screen: a cyan progress slider
/// (the orb color) with a level counter above it. Idempotent.
/// </summary>
public static class SetupHeroHud
{
    private const string ScenePath = "Assets/Scenes/Game.unity";

    [MenuItem("Tools/Monsters and Magic/Setup Hero XP Bar")]
    public static void Setup()
    {
        var previousSetup = EditorSceneManager.GetSceneManagerSetup();
        bool sceneLoaded = EditorSceneManager.GetActiveScene().path == ScenePath;

        if (!sceneLoaded)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        HUDView view = Object.FindFirstObjectByType<HUDView>(FindObjectsInactive.Include);

        if (view == null)
            throw new System.InvalidOperationException("HUDView not found in " + ScenePath);

        Canvas canvas = view.GetComponentInParent<Canvas>();

        if (canvas == null)
            throw new System.InvalidOperationException("HUD canvas not found");

        SerializedObject serialized = new SerializedObject(view);

        // Top stats panel: kills on the first row, level + XP bar in one
        // horizontal row below.
        RectTransform panel = FindOrCreatePanel(canvas);
        RectTransform levelRow = FindOrCreateLevelRow(panel);

        // Level counter above the bar.
        TextMeshProUGUI levelText = (TextMeshProUGUI)serialized
            .FindProperty("levelText").objectReferenceValue;

        if (levelText == null)
        {
            GameObject textObject = new GameObject("LevelText",
                typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(levelRow, false);
            levelText = textObject.GetComponent<TextMeshProUGUI>();
            levelText.text = "Lv 1";
            levelText.fontSize = 28;
            levelText.alignment = TextAlignmentOptions.Center;
            levelText.color = new Color(0.75f, 2.2f, 2.8f, 1f);

            LayoutElement levelLayout = textObject.AddComponent<LayoutElement>();
            levelLayout.minWidth = 70f;
            levelLayout.preferredWidth = 70f;
            levelLayout.minHeight = 30f;
            levelLayout.preferredHeight = 30f;
            ResetForLayout(levelText.rectTransform);

            serialized.FindProperty("levelText").objectReferenceValue = levelText;
        }
        else if (levelText.transform.parent != levelRow)
        {
            levelText.transform.SetParent(levelRow, false);
            ResetForLayout(levelText.rectTransform);
        }

        // The XP slider at the top, tinted like the orbs.
        Slider slider = (Slider)serialized.FindProperty("xpSlider").objectReferenceValue;

        if (slider == null)
        {
            GameObject sliderObject = new GameObject("XpSlider",
                typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(levelRow, false);
            slider = sliderObject.GetComponent<Slider>();
            ResetForLayout(slider.GetComponent<RectTransform>());
            serialized.FindProperty("xpSlider").objectReferenceValue = slider;
        }

        if (slider.transform.parent != levelRow)
        {
            slider.transform.SetParent(levelRow, false);
            ResetForLayout(slider.GetComponent<RectTransform>());
        }

        LayoutElement sliderLayout = slider.gameObject.GetComponent<LayoutElement>();

        if (sliderLayout == null)
            sliderLayout = slider.gameObject.AddComponent<LayoutElement>();

        sliderLayout.minWidth = 420f;
        sliderLayout.preferredWidth = 420f;
        sliderLayout.minHeight = 18f;
        sliderLayout.preferredHeight = 18f;

        // Kills counter joins the panel.
        TextMeshProUGUI killText = (TextMeshProUGUI)serialized
            .FindProperty("killCounterText").objectReferenceValue;

        if (killText != null && killText.transform.parent != panel)
        {
            killText.transform.SetParent(panel, false);
            ResetForLayout(killText.rectTransform);
        }

        if (killText != null)
            killText.transform.SetSiblingIndex(0);

        if (levelRow != null)
            levelRow.SetSiblingIndex(1);

        LayoutElement killLayout = killText != null
            ? killText.GetComponent<LayoutElement>()
            : null;

        if (killText != null && killLayout == null)
        {
            killLayout = killText.gameObject.AddComponent<LayoutElement>();
            killLayout.minHeight = 30f;
            killLayout.preferredHeight = 30f;
        }

        EnsureSliderVisuals(slider);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(view);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        if (!sceneLoaded && previousSetup != null && previousSetup.Length > 0)
            EditorSceneManager.RestoreSceneManagerSetup(previousSetup);

        Debug.Log("Hero XP bar ready at the top of the screen.");
    }

    private static void EnsureSliderVisuals(Slider slider)
    {
        if (slider.fillRect == null)
        {
            GameObject fill = new GameObject("Fill",
                typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(slider.transform, false);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.color = new Color(0.55f, 2.4f, 3.2f, 0.95f);
            fillImage.raycastTarget = false;

            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.sizeDelta = new Vector2(0f, 0f);

            GameObject background = new GameObject("Background",
                typeof(RectTransform), typeof(Image));
            background.transform.SetParent(slider.transform, false);
            background.transform.SetSiblingIndex(0);
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = new Color(0.06f, 0.08f, 0.16f, 0.85f);
            backgroundImage.raycastTarget = false;

            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.sizeDelta = Vector2.zero;

            SerializedObject sliderSerialized = new SerializedObject(slider);
            sliderSerialized.FindProperty("m_FillRect").objectReferenceValue = fillRect;
            sliderSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.interactable = false;
    }

    private static RectTransform FindOrCreatePanel(Canvas canvas)
    {
        Transform existing = canvas.transform.Find("TopStatsPanel");

        if (existing != null)
        {
            RectTransform existingRect = (RectTransform)existing;
            existingRect.anchorMin = new Vector2(0f, 1f);
            existingRect.anchorMax = new Vector2(0f, 1f);
            existingRect.pivot = new Vector2(0f, 1f);
            existingRect.anchoredPosition = new Vector2(16f, -16f);

            VerticalLayoutGroup existingLayout = existing.GetComponent<VerticalLayoutGroup>();

            if (existingLayout != null)
                existingLayout.childAlignment = TextAnchor.MiddleLeft;

            return existingRect;
        }

        GameObject panelObject = new GameObject("TopStatsPanel",
            typeof(RectTransform), typeof(VerticalLayoutGroup));
        panelObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(16f, -16f);
        rect.sizeDelta = new Vector2(560f, 96f);

        VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 6f;

        return rect;
    }

    private static RectTransform FindOrCreateLevelRow(RectTransform panel)
    {
        Transform existing = panel.Find("LevelRow");

        if (existing != null)
            return (RectTransform)existing;

        GameObject rowObject = new GameObject("LevelRow",
            typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowObject.transform.SetParent(panel, false);

        RectTransform rect = rowObject.GetComponent<RectTransform>();
        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 10f;

        LayoutElement rowLayout = rowObject.AddComponent<LayoutElement>();
        rowLayout.minHeight = 30f;
        rowLayout.preferredHeight = 30f;

        return rect;
    }

    /// <summary>
    /// Layout groups drive child rects; stale anchors, pivots and sizes from
    /// the previous manual placement fight with the layout otherwise.
    /// </summary>
    private static void ResetForLayout(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }
}
