using UnityEngine;

/// <summary>
/// 2D audio for hit sounds (headshot, bodyshot).
/// One shared AudioSource, PlayOneShot handles overlapping.
/// Picks a random clip from a list each time.
/// Place on a persistent GameObject in the scene.
/// </summary>
public class HitAudio : MonoBehaviour
{
    public static HitAudio Instance;

    [Header("Headshot")]
    public AudioClip[] headshotClips;
    [Range(0f, 1f)] public float headshotVolume = 1f;

    [Header("Bodyshot")]
    public AudioClip[] bodyshotClips;
    [Range(0f, 1f)] public float bodyshotVolume = 1f;

    AudioSource src;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D
    }

    public void PlayHeadshot()
    {
        Play(headshotClips, headshotVolume);
    }

    public void PlayBodyshot()
    {
        Play(bodyshotClips, bodyshotVolume);
    }

    void Play(AudioClip[] clips, float volume)
    {
        if (clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip != null) src.PlayOneShot(clip, volume);
    }
}