using UnityEngine;

public class PlayerMovementAudio : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Jump")]
    public AudioClip jumpClip;
    public float jumpVolume = 1f;

    [Header("Slide")]
    public AudioClip slideClip;
    public float slideVolume = 1f;

    [Header("Dash")]
    public AudioClip dashClip;
    public float dashVolume = 1f;

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
        if (audioSource == null) return;
        audioSource.loop = false;
        audioSource.Stop();
    }

    public void EnsureSlideAudioPlaying()
    {
        if (audioSource == null || slideClip == null) return;
        if (audioSource.isPlaying && audioSource.clip == slideClip) return;
        audioSource.clip = slideClip;
        audioSource.loop = true;
        audioSource.volume = slideVolume;
        audioSource.Play();
    }

    public void PlayDash()
    {
        if (audioSource == null || dashClip == null) return;
        audioSource.PlayOneShot(dashClip, dashVolume);
    }
}