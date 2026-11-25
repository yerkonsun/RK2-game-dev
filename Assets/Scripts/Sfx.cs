using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>Persistent audio service. All clips are routed through the AudioMixer.</summary>
public sealed class Sfx : MonoBehaviour
{
    const string MixerResource = "Audio/GameAudioMixer";
    const string MusicPref = "audio.music";
    const string SfxPref = "audio.sfx";
    const string MusicParameter = "MusicVolume";
    const string SfxParameter = "SFXVolume";

    static Sfx instance;
    AudioSource sfxSource;
    AudioSource[] musicSources;
    int activeMusicSource;
    AudioClip requestedMusic;
    Coroutine musicRoutine;
    AudioMixer mixer;
    AudioMixerSnapshot normalSnapshot;
    AudioMixerSnapshot pausedSnapshot;
    GameObject settingsCanvas;

    public static void Play(string clipName, float volume = 1f)
    {
        var audio = EnsureInstance();
        var clip = Resources.Load<AudioClip>("Audio/" + clipName);
        if (clip == null)
        {
            Debug.LogWarning("Sound clip not found in Resources/Audio: " + clipName);
            return;
        }
        audio.sfxSource.pitch = Random.Range(0.92f, 1.08f);
        audio.sfxSource.PlayOneShot(clip, volume);
    }

    public static void Music(string clipName)
    {
        var audio = EnsureInstance();
        var clip = Resources.Load<AudioClip>("Audio/" + clipName);
        if (clip == null)
        {
            Debug.LogWarning("Music clip not found in Resources/Audio: " + clipName);
            return;
        }
        if (audio.requestedMusic == clip && audio.musicSources.Length > 0 &&
            ((audio.musicSources[0].clip == clip && audio.musicSources[0].isPlaying) ||
             (audio.musicSources[1].clip == clip && audio.musicSources[1].isPlaying))) return;
        if (audio.musicRoutine != null) audio.StopCoroutine(audio.musicRoutine);
        audio.requestedMusic = clip;
        audio.musicRoutine = audio.StartCoroutine(audio.CrossfadeTo(clip, 1f));
    }

    public static float MusicVolume { get { return PlayerPrefs.GetFloat(MusicPref, 0.4f); } }
    public static float SfxVolume { get { return PlayerPrefs.GetFloat(SfxPref, 0.7f); } }

    public static void SetMusicVolume(float normalized)
    {
        EnsureInstance().SetMixerVolume(MusicParameter, Mathf.Clamp01(normalized), MusicPref);
    }

    public static void SetSfxVolume(float normalized)
    {
        EnsureInstance().SetMixerVolume(SfxParameter, Mathf.Clamp01(normalized), SfxPref);
    }

    public static void SetPaused(bool paused)
    {
        var audio = EnsureInstance();
        if (paused && audio.pausedSnapshot != null) audio.pausedSnapshot.TransitionTo(0.15f);
        else if (audio.normalSnapshot != null) audio.normalSnapshot.TransitionTo(0.2f);
    }

    static Sfx EnsureInstance()
    {
        if (instance != null) return instance;
        var go = new GameObject("[Audio]");
        instance = go.AddComponent<Sfx>();
        DontDestroyOnLoad(go);
        return instance;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        mixer = Resources.Load<AudioMixer>(MixerResource);
        if (mixer == null) Debug.LogError("Missing AudioMixer at Assets/Resources/Audio/GameAudioMixer.mixer. Run Tools > Sound Pass > Build Audio Mixer in the Editor.");

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.outputAudioMixerGroup = FindGroup("SFX");

        musicSources = new AudioSource[2];
        for (int i = 0; i < musicSources.Length; i++)
        {
            musicSources[i] = gameObject.AddComponent<AudioSource>();
            musicSources[i].playOnAwake = false;
            musicSources[i].loop = true;
            musicSources[i].volume = 0f;
            musicSources[i].outputAudioMixerGroup = FindGroup("Music");
        }
        if (mixer != null)
        {
            normalSnapshot = mixer.FindSnapshot("Normal");
            pausedSnapshot = mixer.FindSnapshot("Paused");
        }
        ApplyMixerVolume(MusicParameter, MusicVolume);
        ApplyMixerVolume(SfxParameter, SfxVolume);

        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        if (instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    AudioMixerGroup FindGroup(string groupName)
    {
        if (mixer == null) return null;
        var groups = mixer.FindMatchingGroups(groupName);
        return groups.Length > 0 ? groups[0] : null;
    }

    void SetMixerVolume(string parameter, float normalized, string pref)
    {
        PlayerPrefs.SetFloat(pref, normalized);
        PlayerPrefs.Save();
        ApplyMixerVolume(parameter, normalized);
    }

    void ApplyMixerVolume(string parameter, float normalized)
    {
        if (mixer == null) return;
        float db = normalized <= 0.0001f ? -80f : Mathf.Clamp(20f * Mathf.Log10(normalized), -80f, 0f);
        if (!mixer.SetFloat(parameter, db))
            Debug.LogWarning("AudioMixer parameter is not exposed: " + parameter);
    }

    IEnumerator CrossfadeTo(AudioClip clip, float duration)
    {
        int oldIndex = musicSources[0].volume >= musicSources[1].volume ? 0 : 1;
        int newIndex = 1 - oldIndex;
        AudioSource oldSource = musicSources[oldIndex];
        AudioSource newSource = musicSources[newIndex];
        float oldStartVolume = oldSource.isPlaying ? oldSource.volume : 0f;
        newSource.Stop();
        newSource.clip = clip;
        newSource.volume = 0f;
        newSource.Play();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            newSource.volume = t;
            oldSource.volume = oldStartVolume * (1f - t);
            yield return null;
        }
        newSource.volume = 1f;
        oldSource.Stop();
        oldSource.volume = 0f;
        activeMusicSource = newIndex;
        musicRoutine = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (settingsCanvas != null) Destroy(settingsCanvas);
        settingsCanvas = AudioSettingsUI.Build(gameObject, scene.name == "MainMenu");
    }
}
