using UnityEngine;
using UnityEngine.UI;

// Menu button: loads a scene or quits the game.
[RequireComponent(typeof(Button))]
public class UIButton : MonoBehaviour
{
    public enum Action { LoadScene, Quit }
    public Action action = Action.LoadScene;
    public string sceneName = "Level1";

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        Sfx.Play("click");
        if (action == Action.LoadScene)
        {
            SceneLoader.Load(sceneName);
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
