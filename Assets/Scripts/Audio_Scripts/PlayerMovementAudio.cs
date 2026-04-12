using UnityEngine;

public class PlayerMovementAudio : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Clips")]
    public AudioClip jumpClip;
    public AudioClip slideClip;

    [Header("Volume")]
    public float jumpVolume = 1f;
    public float slideVolume = 1f;

    public void PlayJump()
    {
        if (audioSource == null || jumpClip == null) return;
        audioSource.PlayOneShot(jumpClip, jumpVolume);
    }

    public void PlaySlide()
    {
        if (audioSource == null || slideClip == null) return;
        audioSource.clip = slideClip;
        audioSource.loop = true;
        audioSource.volume = slideVolume;
        audioSource.Play();
    }

    public void StopSlide()
    {
        if (audioSource == null || !audioSource.isPlaying) return;
        audioSource.loop = false;
        audioSource.Stop();
    }
}