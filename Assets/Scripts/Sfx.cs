using UnityEngine;

public static class Sfx
{
    static AudioSource sfxSource;
    static AudioSource musicSource;

    const float MusicVolume = 0.28f;

    static void Init()
    {
        if (sfxSource != null) return;
        var go = new GameObject("[Audio]");
        Object.DontDestroyOnLoad(go);
        sfxSource = go.AddComponent<AudioSource>();
        musicSource = go.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = MusicVolume;
    }

    public static void Play(string clipName, float volume = 1f)
    {
        Init();
        var clip = Resources.Load<AudioClip>("Audio/" + clipName);
        if (clip != null) sfxSource.PlayOneShot(clip, volume);
    }

    public static void Music(string clipName)
    {
        Init();
        var clip = Resources.Load<AudioClip>("Audio/" + clipName);
        if (clip == null || musicSource.clip == clip) return;
        musicSource.clip = clip;
        musicSource.Play();
    }
}
