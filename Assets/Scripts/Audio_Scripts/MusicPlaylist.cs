using UnityEngine;
using System.Collections.Generic;

public class MusicPlaylist : MonoBehaviour
{
    public static MusicPlaylist Instance { get; private set; }

    [Header("Settings")]
    public List<AudioClip> tracks = new List<AudioClip>();

    [Header("References")]
    public AudioSource audioSource;

    List<int> remaining = new List<int>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        PlayNext();
    }

    void Update()
    {
        if (!audioSource.isPlaying)
            PlayNext();
    }

    void PlayNext()
    {
        if (tracks.Count == 0) return;

        if (remaining.Count == 0)
            Refill();

        int pick = Random.Range(0, remaining.Count);
        int index = remaining[pick];
        remaining.RemoveAt(pick);

        audioSource.clip = tracks[index];
        audioSource.Play();
    }

    void Refill()
    {
        remaining.Clear();
        for (int i = 0; i < tracks.Count; i++)
            remaining.Add(i);
    }
}