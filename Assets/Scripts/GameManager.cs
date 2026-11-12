using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int crystalsNeeded = 6;
    public string nextScene = "Level2";
    public string musicName = "music_game";
    public string startHint = "";

    public int Crystals { get; private set; }
    public bool PortalOpen { get { return Crystals >= crystalsNeeded; } }

    public event Action<int, int> CrystalsChanged;
    public event Action PortalOpened;
    public event Action<string> Message;

    bool ending;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        Sfx.Music(musicName);
        if (!string.IsNullOrEmpty(startHint)) StartCoroutine(HintRoutine());
    }

    IEnumerator HintRoutine()
    {
        yield return new WaitForSeconds(0.6f);
        Say(startHint);
    }

    public void Say(string text)
    {
        Message?.Invoke(text);
    }

    public void AddCrystal()
    {
        Crystals++;
        Sfx.Play("pickup");
        CrystalsChanged?.Invoke(Crystals, crystalsNeeded);
        if (Crystals == crystalsNeeded)
        {
            Sfx.Play("portal");
            Say("The temple door is open!");
            PortalOpened?.Invoke();
        }
    }

    public void LevelComplete()
    {
        if (ending) return;
        ending = true;
        Sfx.Play("win");
        SceneLoader.Load(nextScene);
    }

    public void PlayerDied()
    {
        if (ending) return;
        ending = true;
        Sfx.Play("lose");
        StartCoroutine(RestartRoutine());
    }

    IEnumerator RestartRoutine()
    {
        Say("The temple claimed you... try again!");
        yield return new WaitForSeconds(1.4f);
        SceneLoader.Load(SceneManager.GetActiveScene().name);
    }

    public void BossDefeated()
    {
        if (ending) return;
        ending = true;
        StartCoroutine(TreasureRoutine());
    }

    IEnumerator TreasureRoutine()
    {
        Sfx.Play("win");
        var treasure = FindAnyObjectByType<Treasure>();
        if (treasure != null)
        {
            treasure.Reveal();
            yield return new WaitForSeconds(1.2f);
            Say("The guardian is defeated! The treasure is yours!");
            yield return new WaitForSeconds(3.5f);
        }
        SceneLoader.Load("Win");
    }
}
