using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Pause menu: the "pause" button in the HUD calls Pause(), the three stone buttons call
// Resume() / Restart() / LoadMenu(). Esc / P toggles the menu on a keyboard.
// The panel pops in with an animation that runs on unscaled time (the game itself is frozen).
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private RectTransform popup;      // the stone window that scales in (optional)
    [SerializeField] private string menuSceneName = "MainMenu";

    bool paused;
    Coroutine popRoutine;

    private void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        pausePanel.SetActive(false);
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
        AudioListener.pause = true;
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
        AudioListener.pause = false;
        Sfx.Play("click");
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
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
