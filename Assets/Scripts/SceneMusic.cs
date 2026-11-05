using UnityEngine;

// Starts a music track when a non-level scene (menu, win screen) loads.
public class SceneMusic : MonoBehaviour
{
    public string musicName = "music_menu";

    void Start()
    {
        Application.targetFrameRate = 60;
        Time.timeScale = 1f;
        Sfx.Music(musicName);
    }
}
