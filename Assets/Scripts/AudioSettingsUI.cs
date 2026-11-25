using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds the pixel-styled audio control and settings card.</summary>
public static class AudioSettingsUI
{
    static GameObject panel;
    static Transform panelContent;

    static readonly Color Gold = new Color(1f, 0.82f, 0.38f, 1f);
    static readonly Color Cream = new Color(0.96f, 0.87f, 0.68f, 1f);
    static readonly Color Ink = new Color(0.20f, 0.13f, 0.25f, 1f);
    static Sprite panelSprite;
    static Sprite buttonSprite;

    public static GameObject Build(GameObject owner, bool mainMenu)
    {
        panelSprite = Resources.Load<Sprite>("AudioUI/panel");
        buttonSprite = Resources.Load<Sprite>("AudioUI/button");

        var root = new GameObject("Audio Settings Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Object.DontDestroyOnLoad(owner);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        panel = MakePanel(root.transform);
        panel.SetActive(false);
        var open = MakeButton(root.transform, "AudioSettingsButton", "", new Vector2(1f, 1f), new Vector2(-150f, -205f), new Vector2(190f, 76f));
        AddSpeakerIcon(open.transform);
        open.onClick.AddListener(() => panel.SetActive(!panel.activeSelf));

        AddText(panelContent, "Title", "SOUND SETTINGS", 42, new Vector2(0f, 224f), new Vector2(680f, 58f), Gold, true);
        AddText(panelContent, "Subtitle", "TUNE THE TEMPLE'S SOUND", 18, new Vector2(0f, 185f), new Vector2(600f, 30f), Cream, false);
        MakeImage(panelContent, "TitleDivider", Gold, new Vector2(0f, 154f), new Vector2(240f, 4f));

        AddText(panelContent, "MusicLabel", "MUSIC", 25, new Vector2(0f, 106f), new Vector2(560f, 38f), Cream, true);
        var music = MakeSlider(panelContent, "MusicSlider", new Vector2(0f, 62f));
        music.SetValueWithoutNotify(Sfx.MusicVolume);
        music.onValueChanged.AddListener(Sfx.SetMusicVolume);

        AddText(panelContent, "SfxLabel", "SOUND EFFECTS", 25, new Vector2(0f, -14f), new Vector2(560f, 38f), Cream, true);
        var effects = MakeSlider(panelContent, "SfxSlider", new Vector2(0f, -58f));
        effects.SetValueWithoutNotify(Sfx.SfxVolume);
        effects.onValueChanged.AddListener(Sfx.SetSfxVolume);

        var close = MakeButton(panelContent, "CloseButton", "CLOSE", new Vector2(0.5f, 0.5f), new Vector2(0f, -190f), new Vector2(300f, 66f));
        close.onClick.AddListener(() => panel.SetActive(false));
        AddText(panelContent, "SaveHint", "Changes are saved automatically", 17, new Vector2(0f, -237f), new Vector2(560f, 28f), Cream, false);

        var updater = root.AddComponent<AudioSettingsVisibility>();
        updater.Initialize(open.gameObject, mainMenu);
        return root;
    }

    static GameObject MakePanel(Transform parent)
    {
        var overlay = new GameObject("SettingsOverlay", typeof(RectTransform));
        overlay.transform.SetParent(parent, false);
        Stretch(overlay.GetComponent<RectTransform>());

        var dim = new GameObject("SettingsDim", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(overlay.transform, false);
        Stretch(dim.GetComponent<RectTransform>());
        var dimImage = dim.GetComponent<Image>();
        dimImage.color = new Color(0.025f, 0.012f, 0.045f, 0.76f);
        dimImage.raycastTarget = true;

        var frame = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(overlay.transform, false);
        var frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(800f, 620f);
        var image = frame.GetComponent<Image>();
        image.sprite = panelSprite;
        image.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = panelSprite != null ? Color.white : new Color(0.075f, 0.045f, 0.12f, 1f);

        var content = new GameObject("PanelContent", typeof(RectTransform));
        content.transform.SetParent(frame.transform, false);
        var rect = content.GetComponent<RectTransform>();
        Stretch(rect);
        panelContent = content.transform;
        return overlay;
    }

    static Slider MakeSlider(Transform parent, string name, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(560f, 34f);
        var slider = go.GetComponent<Slider>();
        var background = MakeImage(go.transform, "Background", new Color(0.18f, 0.14f, 0.24f, 1f), Vector2.zero, new Vector2(560f, 18f));
        // The track and handle must receive pointer events so Slider can begin dragging.
        background.raycastTarget = true;
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var fr = fillArea.GetComponent<RectTransform>();
        fr.anchorMin = new Vector2(0f, 0.25f); fr.anchorMax = new Vector2(1f, 0.75f); fr.offsetMin = new Vector2(8f, 0f); fr.offsetMax = new Vector2(-8f, 0f);
        var fill = MakeImage(fillArea.transform, "Fill", Gold, Vector2.zero, Vector2.zero);
        Stretch(fill.rectTransform);
        fill.raycastTarget = false;
        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        var hr = handleArea.GetComponent<RectTransform>();
        hr.anchorMin = Vector2.zero; hr.anchorMax = Vector2.one; hr.offsetMin = new Vector2(8f, 0f); hr.offsetMax = new Vector2(-8f, 0f);
        var handle = MakeImage(handleArea.transform, "Handle", Gold, Vector2.zero, new Vector2(26f, 38f));
        handle.raycastTarget = true;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.minValue = 0f; slider.maxValue = 1f; slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        return slider;
    }

    static Button MakeButton(Transform parent, string name, string caption, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.sprite = buttonSprite;
        image.type = buttonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = buttonSprite != null ? Color.white : new Color(0.30f, 0.20f, 0.46f, 1f);
        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.88f, 0.63f, 1f);
        colors.pressedColor = new Color(0.87f, 0.65f, 0.33f, 1f);
        button.colors = colors;
        if (!string.IsNullOrEmpty(caption)) AddText(go.transform, "Label", caption, 23, Vector2.zero, size, Ink, true);
        return button;
    }

    static void AddSpeakerIcon(Transform parent)
    {
        var icon = new GameObject("SpeakerIcon", typeof(RectTransform));
        icon.transform.SetParent(parent, false);
        var iconRect = icon.GetComponent<RectTransform>();
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(72f, 46f);

        MakeImage(icon.transform, "SpeakerBody", Ink, new Vector2(-15f, 0f), new Vector2(14f, 22f));
        var cone = MakeImage(icon.transform, "SpeakerCone", Ink, new Vector2(-3f, 0f), new Vector2(20f, 28f));
        cone.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        MakeImage(icon.transform, "SoundWaveNear", Ink, new Vector2(16f, 0f), new Vector2(5f, 25f)).rectTransform.localRotation = Quaternion.Euler(0f, 0f, 18f);
        MakeImage(icon.transform, "SoundWaveFar", Ink, new Vector2(26f, 0f), new Vector2(5f, 36f)).rectTransform.localRotation = Quaternion.Euler(0f, 0f, 18f);
    }

    static Text AddText(Transform parent, string name, string value, int fontSize, Vector2 position, Vector2 size, Color color, bool bold)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    static Image MakeImage(Transform parent, string name, Color color, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}

public sealed class AudioSettingsVisibility : MonoBehaviour
{
    GameObject button;
    bool mainMenu;

    public void Initialize(GameObject settingsButton, bool isMainMenu)
    {
        button = settingsButton;
        mainMenu = isMainMenu;
    }

    void Update()
    {
        if (button != null) button.SetActive(mainMenu || Time.timeScale == 0f);
    }
}
