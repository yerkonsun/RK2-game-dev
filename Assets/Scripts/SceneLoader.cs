using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    static SceneLoader instance;

    Image overlay;
    bool busy;

    public static void Load(string sceneName)
    {
        if (instance == null)
        {
            var go = new GameObject("[SceneLoader]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SceneLoader>();
            instance.BuildOverlay();
        }
        instance.StartLoad(sceneName);
    }

    void BuildOverlay()
    {
        var canvasGo = new GameObject("FadeCanvas");
        canvasGo.transform.SetParent(transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        var imgGo = new GameObject("Black");
        imgGo.transform.SetParent(canvasGo.transform, false);
        overlay = imgGo.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0);
        overlay.raycastTarget = false;
        var rt = overlay.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    void StartLoad(string sceneName)
    {
        if (busy) return;
        StartCoroutine(LoadRoutine(sceneName));
    }

    IEnumerator LoadRoutine(string sceneName)
    {
        busy = true;
        yield return Fade(0f, 1f, 0.3f);
        SceneManager.LoadScene(sceneName);
        yield return null;
        yield return Fade(1f, 0f, 0.3f);
        busy = false;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            overlay.color = new Color(0, 0, 0, Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        overlay.color = new Color(0, 0, 0, to);
    }
}
