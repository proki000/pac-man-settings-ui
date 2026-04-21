using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class SettingsOverlay : MonoBehaviour
{
    static SettingsOverlay instance;

    readonly List<Action> refreshers = new List<Action>();
    readonly int[] frameRateOptions = { 30, 60, 90, 120, -1 };
    readonly string[] frameRateLabels = { "30", "60", "90", "120", "Unlimited" };
    readonly string[] windowPresetLabels = { "Native", "1280 x 720", "1600 x 900", "1920 x 1080" };

    Canvas canvas;
    CanvasScaler canvasScaler;
    RectTransform uiRoot;
    Font font;
    Sprite uiSprite;
    DefaultControls.Resources uiResources;

    GameObject panel;
    GameObject dpad;
    Text modeText;
    Text fpsText;
    RectTransform settingsContent;

    float fpsTimer;
    int fpsFrameCount;
    int fpsValue;
    float visualRefreshTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;

        GameSettings.Load();
        GameObject overlay = new GameObject("Pac-man Settings Overlay");
        DontDestroyOnLoad(overlay);
        overlay.AddComponent<SettingsOverlay>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        GameSettings.Load();
        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        CreateResources();
        BuildOverlay();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        GameSettings.SettingsChanged += HandleSettingsChanged;
        RefreshAll();
        ApplyOverlayScale();
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            GameSettings.SettingsChanged -= HandleSettingsChanged;
            instance = null;
        }
    }

    void Update()
    {
        GameSettings.ApplySystemSettings();

        if (Input.GetKeyDown(KeyCode.F1))
            TogglePanel();

        UpdateFps();
        visualRefreshTimer += Time.unscaledDeltaTime;
        if (visualRefreshTimer >= 1f)
        {
            visualRefreshTimer = 0f;
            if (GameSettings.ColorblindPalette)
                ApplySceneVisuals();
        }
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameSettings.SetTouchDirection(Vector2.zero);
        StartCoroutine(ApplyVisualsAfterSceneSettles());
    }

    IEnumerator ApplyVisualsAfterSceneSettles()
    {
        yield return null;
        yield return null;
        ApplySceneVisuals();
    }

    void HandleSettingsChanged()
    {
        RefreshAll();
        ApplyOverlayScale();
        ApplySceneVisuals();
    }

    void CreateResources()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        uiSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        uiResources = new DefaultControls.Resources
        {
            standard = uiSprite,
            background = uiSprite,
            inputField = uiSprite,
            knob = uiSprite,
            checkmark = uiSprite,
            dropdown = uiSprite,
            mask = uiSprite
        };
    }

    void BuildOverlay()
    {
        GameObject canvasObject = new GameObject("Pac-man Settings Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.matchWidthOrHeight = 0.5f;

        uiRoot = CreateRect("Settings Root", canvasObject.transform);
        Stretch(uiRoot);

        BuildTopBar();
        BuildPanel();
        BuildDpad();
        BuildFpsLabel();
        EnsureEventSystem();
    }

    void BuildTopBar()
    {
        GameObject bar = CreatePanel("Settings Bar", uiRoot, new Color(0.01f, 0.01f, 0.04f, 0.92f));
        RectTransform rect = bar.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 54f);

        HorizontalLayoutGroup layout = bar.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 7, 7);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = true;

        Button settingsButton = CreateButton(bar.transform, "SETTINGS", new Color(1f, 0.82f, 0.12f, 1f), TogglePanel);
        SetPreferredSize(settingsButton.gameObject, 142f, 40f);

        modeText = CreateText("Mode Text", bar.transform, "MODE: CLASSIC", 17, TextAnchor.MiddleLeft, new Color(0.76f, 0.92f, 1f, 1f));
        SetPreferredSize(modeText.gameObject, 310f, 40f, flexibleWidth: 1f);

        Button resetButton = CreateButton(bar.transform, "RESET", new Color(0.2f, 0.72f, 1f, 1f), () =>
        {
            GameSettings.ResetAll();
            RefreshAll();
        });
        SetPreferredSize(resetButton.gameObject, 100f, 40f);
    }

    void BuildPanel()
    {
        panel = CreatePanel("Settings Panel", uiRoot, new Color(0.02f, 0.02f, 0.07f, 0.96f));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-16f, -70f);
        rect.sizeDelta = new Vector2(560f, 780f);

        GameObject header = new GameObject("Settings Header", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        header.transform.SetParent(panel.transform, false);
        header.GetComponent<LayoutElement>().preferredHeight = 44f;
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, -14f);
        headerRect.sizeDelta = new Vector2(-32f, 48f);
        HorizontalLayoutGroup headerLayout = header.GetComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 8f;
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childControlWidth = false;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandHeight = true;

        Text title = CreateText("Title", header.transform, "PAC-MAN SETTINGS", 22, TextAnchor.MiddleLeft, new Color(1f, 0.86f, 0.12f, 1f));
        SetPreferredSize(title.gameObject, 360f, 40f, flexibleWidth: 1f);

        Button close = CreateButton(header.transform, "CLOSE", new Color(1f, 0.29f, 0.47f, 1f), TogglePanel);
        SetPreferredSize(close.gameObject, 96f, 38f);

        GameObject scrollObject = CreatePanel("Settings Scroll", panel.transform, new Color(0f, 0f, 0f, 0.2f));
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.pivot = new Vector2(0.5f, 0.5f);
        scrollRect.offsetMin = new Vector2(16f, 16f);
        scrollRect.offsetMax = new Vector2(-16f, -76f);

        ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.scrollSensitivity = 26f;

        GameObject viewportObject = new GameObject("Settings Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        Stretch(viewport);

        GameObject contentObject = new GameObject("Settings Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewportObject.transform, false);
        RectTransform content = contentObject.GetComponent<RectTransform>();
        settingsContent = content;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.offsetMin = new Vector2(0f, content.offsetMin.y);
        content.offsetMax = new Vector2(0f, content.offsetMax.y);
        content.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(6, 6, 6, 6);
        contentLayout.spacing = 9f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport;
        scroll.content = content;

        AddSection(content, "Modes");
        AddModeDropdown(content);

        AddSection(content, "Gameplay");
        AddSlider(content, "Game speed", 0.5f, 2f, () => GameSettings.GameSpeedMultiplier, GameSettings.SetGameSpeed, Percent);
        AddSlider(content, "Pac-man speed", 0.5f, 2f, () => GameSettings.PacmanSpeedMultiplier, GameSettings.SetPacmanSpeed, Percent);
        AddSlider(content, "Ghost speed", 0.5f, 2f, () => GameSettings.GhostSpeedMultiplier, GameSettings.SetGhostSpeed, Percent);
        AddSlider(content, "Fright time", 0f, 2.5f, () => GameSettings.FrightDurationMultiplier, GameSettings.SetFrightDuration, Percent);
        AddSlider(content, "Starting lives", 1f, 6f, () => GameSettings.StartingLives, v => GameSettings.SetStartingLives(Mathf.RoundToInt(v)), WholeNumber, wholeNumbers: true);
        AddToggle(content, "Invincible", () => GameSettings.Invincible, GameSettings.SetInvincible);
        AddToggle(content, "Touch D-pad", () => GameSettings.TouchControls, GameSettings.SetTouchControls);

        AddSection(content, "Audio");
        AddSlider(content, "Master volume", 0f, 1f, () => GameSettings.MasterVolume, GameSettings.SetMasterVolume, Percent);

        AddSection(content, "Display");
        AddDropdown(content, "Target FPS", frameRateLabels, CurrentFrameRateIndex, index => GameSettings.SetTargetFrameRate(frameRateOptions[index]));
        AddToggle(content, "VSync", () => GameSettings.VSync, GameSettings.SetVSync);
        AddToggle(content, "Fullscreen", () => GameSettings.Fullscreen, GameSettings.SetFullscreen);
        AddQualityDropdown(content);
        AddDropdown(content, "Window size", windowPresetLabels, () => GameSettings.WindowPreset, GameSettings.SetWindowPreset);
        AddSlider(content, "UI scale", 0.75f, 1.4f, () => GameSettings.UiScale, GameSettings.SetUiScale, Percent);

        AddSection(content, "Accessibility");
        AddToggle(content, "Show FPS", () => GameSettings.ShowFps, GameSettings.SetShowFps);
        AddToggle(content, "Reduce flashing", () => GameSettings.ReduceFlashing, GameSettings.SetReduceFlashing);
        AddToggle(content, "Color assist", () => GameSettings.ColorblindPalette, GameSettings.SetColorblindPalette);

        Text note = CreateText("Restart Note", content, "Lives and mode presets apply cleanly when you start a new run.", 14, TextAnchor.MiddleLeft, new Color(0.7f, 0.76f, 0.88f, 1f));
        note.resizeTextForBestFit = true;
        SetPreferredSize(note.gameObject, 0f, 38f, flexibleWidth: 1f);
        RebuildSettingsLayout();

        panel.SetActive(false);
    }

    void BuildDpad()
    {
        dpad = CreatePanel("Touch D-pad", uiRoot, new Color(0f, 0f, 0f, 0.05f));
        RectTransform rect = dpad.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-28f, 28f);
        rect.sizeDelta = new Vector2(224f, 224f);

        CreatePadButton("UP", new Vector2(0f, 70f), Vector2.up);
        CreatePadButton("LEFT", new Vector2(-70f, 0f), Vector2.left);
        CreatePadButton("RIGHT", new Vector2(70f, 0f), Vector2.right);
        CreatePadButton("DOWN", new Vector2(0f, -70f), Vector2.down);
    }

    void BuildFpsLabel()
    {
        fpsText = CreateText("FPS Label", uiRoot, "0 FPS", 18, TextAnchor.MiddleRight, new Color(0.75f, 1f, 0.55f, 1f));
        RectTransform rect = fpsText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-20f, -62f);
        rect.sizeDelta = new Vector2(160f, 34f);
    }

    void CreatePadButton(string label, Vector2 position, Vector2 direction)
    {
        Button button = CreateButton(dpad.transform, label, new Color(1f, 0.82f, 0.12f, 0.92f), null);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(66f, 58f);

        EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();
        AddTrigger(trigger, EventTriggerType.PointerDown, () => GameSettings.SetTouchDirection(direction));
        AddTrigger(trigger, EventTriggerType.PointerUp, () => GameSettings.SetTouchDirection(Vector2.zero));
        AddTrigger(trigger, EventTriggerType.PointerExit, () => GameSettings.SetTouchDirection(Vector2.zero));
    }

    void AddTrigger(EventTrigger trigger, EventTriggerType type, Action action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }

    void AddModeDropdown(Transform parent)
    {
        AddDropdown(parent, "Play mode", GameSettings.PlayModeNames, () => (int)GameSettings.Mode, index =>
        {
            GameSettings.ApplyMode((GameSettings.PlayMode)index);
            RefreshAll();
        });
    }

    void AddQualityDropdown(Transform parent)
    {
        string[] names = QualitySettings.names;
        if (names == null || names.Length == 0)
            names = new[] { "Low", "Medium", "High" };

        AddDropdown(parent, "Quality", names, () => Mathf.Clamp(GameSettings.QualityLevel, 0, names.Length - 1), GameSettings.SetQualityLevel);
    }

    int CurrentFrameRateIndex()
    {
        for (int i = 0; i < frameRateOptions.Length; i++)
        {
            if (frameRateOptions[i] == GameSettings.TargetFrameRate)
                return i;
        }

        return 1;
    }

    void AddSection(Transform parent, string text)
    {
        Text label = CreateText(text + " Section", parent, text.ToUpperInvariant(), 17, TextAnchor.MiddleLeft, new Color(1f, 0.82f, 0.12f, 1f));
        label.fontStyle = FontStyle.Bold;
        SetPreferredSize(label.gameObject, 0f, 32f, flexibleWidth: 1f);
    }

    void AddSlider(Transform parent, string label, float min, float max, Func<float> getter, Action<float> setter, Func<float, string> formatter, bool wholeNumbers = false)
    {
        GameObject row = CreateRow(parent, label);
        CreateText(label + " Label", row.transform, label, 15, TextAnchor.MiddleLeft, Color.white);

        GameObject sliderObject = DefaultControls.CreateSlider(uiResources);
        sliderObject.name = label + " Slider";
        sliderObject.transform.SetParent(row.transform, false);
        SetFontRecursive(sliderObject);
        SetPreferredSize(sliderObject, 0f, 34f, flexibleWidth: 1f);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;
        ColorSlider(slider);

        Text valueText = CreateText(label + " Value", row.transform, formatter(getter()), 14, TextAnchor.MiddleRight, new Color(0.76f, 0.92f, 1f, 1f));
        SetPreferredSize(valueText.gameObject, 86f, 34f);

        slider.onValueChanged.AddListener(value =>
        {
            setter(value);
            valueText.text = formatter(getter());
        });

        refreshers.Add(() =>
        {
            slider.SetValueWithoutNotify(getter());
            valueText.text = formatter(getter());
        });
    }

    void AddToggle(Transform parent, string label, Func<bool> getter, Action<bool> setter)
    {
        GameObject row = CreateRow(parent, label);
        CreateText(label + " Label", row.transform, label, 15, TextAnchor.MiddleLeft, Color.white);

        GameObject toggleObject = DefaultControls.CreateToggle(uiResources);
        toggleObject.name = label + " Toggle";
        toggleObject.transform.SetParent(row.transform, false);
        SetFontRecursive(toggleObject);
        SetPreferredSize(toggleObject, 0f, 34f, flexibleWidth: 1f);

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        Text toggleLabel = toggleObject.GetComponentInChildren<Text>();
        if (toggleLabel != null)
        {
            toggleLabel.text = getter() ? "ON" : "OFF";
            toggleLabel.color = new Color(0.76f, 0.92f, 1f, 1f);
            toggleLabel.alignment = TextAnchor.MiddleLeft;
        }
        ColorToggle(toggle);

        toggle.onValueChanged.AddListener(value =>
        {
            setter(value);
            if (toggleLabel != null) toggleLabel.text = value ? "ON" : "OFF";
        });

        refreshers.Add(() =>
        {
            toggle.SetIsOnWithoutNotify(getter());
            if (toggleLabel != null) toggleLabel.text = getter() ? "ON" : "OFF";
        });
    }

    void AddDropdown(Transform parent, string label, string[] options, Func<int> getter, Action<int> setter)
    {
        GameObject row = CreateRow(parent, label);
        CreateText(label + " Label", row.transform, label, 15, TextAnchor.MiddleLeft, Color.white);

        GameObject dropdownObject = DefaultControls.CreateDropdown(uiResources);
        dropdownObject.name = label + " Dropdown";
        dropdownObject.transform.SetParent(row.transform, false);
        SetFontRecursive(dropdownObject);
        SetPreferredSize(dropdownObject, 0f, 36f, flexibleWidth: 1f);

        Dropdown dropdown = dropdownObject.GetComponent<Dropdown>();
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>(options));
        dropdown.captionText.color = Color.black;
        dropdown.itemText.color = Color.black;
        ColorDropdown(dropdown);

        dropdown.onValueChanged.AddListener(index => setter(index));

        refreshers.Add(() =>
        {
            int value = Mathf.Clamp(getter(), 0, options.Length - 1);
            dropdown.SetValueWithoutNotify(value);
            dropdown.RefreshShownValue();
        });
    }

    GameObject CreateRow(Transform parent, string name)
    {
        GameObject row = new GameObject(name + " Row", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        Image rowImage = row.GetComponent<Image>();
        rowImage.sprite = uiSprite;
        rowImage.color = new Color(0.08f, 0.09f, 0.16f, 0.82f);

        LayoutElement element = row.GetComponent<LayoutElement>();
        element.preferredHeight = 42f;
        element.minHeight = 42f;

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 4, 4);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = false;

        return row;
    }

    Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);
        Text text = obj.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = size;
        SetPreferredSize(obj, 170f, 34f);
        return text;
    }

    Button CreateButton(Transform parent, string label, Color color, Action action)
    {
        GameObject obj = DefaultControls.CreateButton(uiResources);
        obj.name = label + " Button";
        obj.transform.SetParent(parent, false);
        SetFontRecursive(obj);

        Button button = obj.GetComponent<Button>();
        Image image = obj.GetComponent<Image>();
        image.color = color;
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.22f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);
        button.colors = colors;

        Text text = obj.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = label;
            text.color = Color.black;
            text.fontStyle = FontStyle.Bold;
            text.resizeTextForBestFit = true;
        }

        if (action != null)
            button.onClick.AddListener(() => action());

        return button;
    }

    GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.sprite = uiSprite;
        image.color = color;
        return obj;
    }

    RectTransform CreateRect(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<RectTransform>();
    }

    void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    void SetPreferredSize(GameObject obj, float width, float height, float flexibleWidth = 0f)
    {
        LayoutElement element = obj.GetComponent<LayoutElement>();
        if (element == null) element = obj.AddComponent<LayoutElement>();

        if (width > 0f) element.preferredWidth = width;
        element.preferredHeight = height;
        element.flexibleWidth = flexibleWidth;
    }

    void SetFontRecursive(GameObject obj)
    {
        foreach (Text text in obj.GetComponentsInChildren<Text>(true))
        {
            text.font = font;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 9;
            text.resizeTextMaxSize = Mathf.Max(12, text.fontSize);
        }
    }

    void ColorSlider(Slider slider)
    {
        Image background = slider.transform.Find("Background")?.GetComponent<Image>();
        if (background != null) background.color = new Color(0.18f, 0.22f, 0.3f, 1f);

        Image fill = slider.fillRect?.GetComponent<Image>();
        if (fill != null) fill.color = new Color(1f, 0.82f, 0.12f, 1f);

        Image handle = slider.handleRect?.GetComponent<Image>();
        if (handle != null) handle.color = new Color(0.24f, 0.86f, 1f, 1f);
    }

    void ColorToggle(Toggle toggle)
    {
        Image background = toggle.targetGraphic as Image;
        if (background != null) background.color = new Color(0.18f, 0.22f, 0.3f, 1f);

        Image check = toggle.graphic as Image;
        if (check != null) check.color = new Color(1f, 0.82f, 0.12f, 1f);
    }

    void ColorDropdown(Dropdown dropdown)
    {
        Image image = dropdown.GetComponent<Image>();
        if (image != null) image.color = new Color(0.85f, 0.93f, 1f, 1f);
    }

    string Percent(float value) => Mathf.RoundToInt(value * 100f) + "%";
    string WholeNumber(float value) => Mathf.RoundToInt(value).ToString();

    void TogglePanel()
    {
        panel.SetActive(!panel.activeSelf);
        if (panel.activeSelf)
            RebuildSettingsLayout();
    }

    void RefreshAll()
    {
        if (modeText != null)
            modeText.text = "MODE: " + GameSettings.CurrentModeName().ToUpperInvariant();

        if (dpad != null)
            dpad.SetActive(GameSettings.TouchControls);

        if (fpsText != null)
            fpsText.gameObject.SetActive(GameSettings.ShowFps);

        foreach (Action refresher in refreshers)
            refresher();
    }

    void ApplyOverlayScale()
    {
        if (uiRoot != null)
            uiRoot.localScale = Vector3.one * GameSettings.UiScale;

        RebuildSettingsLayout();
    }

    void RebuildSettingsLayout()
    {
        if (settingsContent == null) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(settingsContent);
        settingsContent.anchoredPosition = new Vector2(settingsContent.anchoredPosition.x, 0f);
    }

    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystem);
    }

    void UpdateFps()
    {
        if (fpsText == null) return;

        fpsText.gameObject.SetActive(GameSettings.ShowFps);
        if (!GameSettings.ShowFps) return;

        fpsFrameCount++;
        fpsTimer += Time.unscaledDeltaTime;
        if (fpsTimer >= 0.35f)
        {
            fpsValue = Mathf.RoundToInt(fpsFrameCount / fpsTimer);
            fpsFrameCount = 0;
            fpsTimer = 0f;
            fpsText.text = fpsValue + " FPS";
        }
    }

    void ApplySceneVisuals()
    {
        Color mazeColor = GameSettings.ColorblindPalette ? new Color(0.52f, 0.86f, 1f, 1f) : Color.white;
        Color dotColor = GameSettings.ColorblindPalette ? new Color(1f, 0.95f, 0.48f, 1f) : Color.white;
        Color fruitColor = GameSettings.ColorblindPalette ? new Color(1f, 0.7f, 0.86f, 1f) : Color.white;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            mainCamera.backgroundColor = GameSettings.ColorblindPalette ? new Color(0f, 0f, 0.02f, 1f) : Color.black;

        foreach (Tilemap tilemap in FindObjectsOfType<Tilemap>())
            tilemap.color = mazeColor;

        foreach (SpriteRenderer sprite in FindObjectsOfType<SpriteRenderer>())
        {
            if (sprite.CompareTag("dot") || sprite.CompareTag("powerDot"))
            {
                sprite.color = dotColor;
            }
            else if (sprite.CompareTag("fruit"))
            {
                sprite.color = fruitColor;
            }
            else if (sprite.gameObject.name.IndexOf("maze", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     sprite.gameObject.name.IndexOf("levelend", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                sprite.color = mazeColor;
            }
            else if (!sprite.CompareTag("ghost") && !sprite.CompareTag("pacman"))
            {
                sprite.color = Color.white;
            }
        }
    }
}
