using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private RectTransform popup; 
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private bool enableStyledPauseMenu;
    [SerializeField] private Sprite pausePanelSprite;
    [SerializeField] private Sprite pauseButtonSprite;

    bool paused;
    Coroutine popRoutine;

    private void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (enableStyledPauseMenu) ApplyPauseStyle();
        pausePanel.SetActive(false);
    }

    void ApplyPauseStyle()
    {
        if (pausePanel == null) return;

        Transform panelTransform = pausePanel.transform;
        if (panelTransform.Find("PauseCard") == null)
        {
            var card = new GameObject("PauseCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(panelTransform, false);
            card.transform.SetAsFirstSibling();
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(620f, 590f);

            var cardImage = card.GetComponent<Image>();
            cardImage.sprite = pausePanelSprite;
            cardImage.type = pausePanelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            cardImage.color = Color.white;
            cardImage.raycastTarget = true;

            AddPauseText(card.transform, "PauseTitle", "PAUSED", 58, new Vector2(0.5f, 0.5f), new Vector2(520f, 78f), new Vector2(0f, 215f), new Color(1f, 0.82f, 0.36f));
            AddPauseText(card.transform, "PauseSubtitle", "THE TEMPLE CAN WAIT", 20, new Vector2(0.5f, 0.5f), new Vector2(500f, 34f), new Vector2(0f, 164f), new Color(0.91f, 0.81f, 0.63f));

            var divider = new GameObject("GoldDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            divider.transform.SetParent(card.transform, false);
            var dividerRect = divider.GetComponent<RectTransform>();
            dividerRect.anchorMin = dividerRect.anchorMax = new Vector2(0.5f, 0.5f);
            dividerRect.anchoredPosition = new Vector2(0f, 137f);
            dividerRect.sizeDelta = new Vector2(260f, 5f);
            divider.GetComponent<Image>().color = new Color(0.91f, 0.62f, 0.20f, 1f);
            divider.GetComponent<Image>().raycastTarget = false;
        }

        StylePauseButton("ResumeButton", 65f);
        StylePauseButton("RestartButton", -35f);
        StylePauseButton("MenuButton", -135f);
    }

    void StylePauseButton(string buttonName, float y)
    {
        Transform buttonTransform = pausePanel.transform.Find(buttonName);
        if (buttonTransform == null) return;

        var rect = buttonTransform.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(420f, 78f);

        var image = buttonTransform.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = pauseButtonSprite;
            image.type = pauseButtonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = Color.white;
            image.preserveAspect = false;
        }

        var button = buttonTransform.GetComponent<Button>();
        if (button != null)
        {
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.88f, 0.63f, 1f);
            colors.pressedColor = new Color(0.87f, 0.65f, 0.33f, 1f);
            colors.selectedColor = new Color(1f, 0.93f, 0.76f, 1f);
            button.colors = colors;
        }

        var label = buttonTransform.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.fontSize = 30;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.19f, 0.13f, 0.23f, 1f);
        }
    }

    static void AddPauseText(Transform parent, string objectName, string value, int size, Vector2 anchor, Vector2 dimensions, Vector2 position, Color color)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)
        {
            if (paused) Resume(); else Pause();
        }
    }

    public void Pause()
    {
        if (paused) return;
        paused = true;
        Sfx.Play("click");
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        Sfx.SetPaused(true);
        if (popup != null)
        {
            if (popRoutine != null) StopCoroutine(popRoutine);
            popRoutine = StartCoroutine(PopIn());
        }
    }

    public void Resume()
    {
        if (!paused) return;
        paused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        Sfx.SetPaused(false);
        Sfx.Play("click");
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        Sfx.SetPaused(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMenu()
    {
        Time.timeScale = 1f;
        Sfx.SetPaused(false);
        SceneManager.LoadScene(menuSceneName);
    }

    IEnumerator PopIn()
    {
        float t = 0f;
        while (t < 0.18f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.18f);
            float s = Mathf.Lerp(0.8f, 1f, 1f - (1f - k) * (1f - k));
            popup.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        popup.localScale = Vector3.one;
    }
}
