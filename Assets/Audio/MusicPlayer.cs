using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    public AudioClip[] playlist;
    AudioSource audioSource;
    int lastIndex = -1;

    void Awake()
    {
        if (FindObjectsByType<MusicPlayer>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (playlist.Length == 0) return;
        if (!audioSource.isPlaying)
            PlayNext();
    }

    void PlayNext()
    {
        if (playlist.Length == 1)
        {
            audioSource.clip = playlist[0];
        }
        else
        {
            int index;
            do { index = Random.Range(0, playlist.Length); }
            while (index == lastIndex);
            lastIndex = index;
            audioSource.clip = playlist[index];
        }
        audioSource.Play();
    }
}