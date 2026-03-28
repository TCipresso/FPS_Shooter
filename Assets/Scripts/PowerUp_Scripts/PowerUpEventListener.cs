using System.Collections;
using UnityEngine;
using TMPro;

public class PowerUpEventListener : MonoBehaviour
{
    [Header("References")]
    public AudioSource sfxSource;
    public AudioSource musicSource;
    public TextMeshProUGUI messageText;

    Coroutine messageCoroutine;

    public void OnPowerUpActivated(PowerUpEvent powerUpEvent)
    {
        if (powerUpEvent == null) return;

        // Play activation sound
        if (sfxSource != null && powerUpEvent.activationSound != null)
            sfxSource.PlayOneShot(powerUpEvent.activationSound);

        // Play music if assigned
        if (musicSource != null && powerUpEvent.musicClip != null)
        {
            musicSource.clip = powerUpEvent.musicClip;
            musicSource.Play();
        }

        // Show message
        if (messageText != null && !string.IsNullOrEmpty(powerUpEvent.displayMessage))
        {
            if (messageCoroutine != null)
                StopCoroutine(messageCoroutine);
            messageCoroutine = StartCoroutine(ShowMessage(powerUpEvent.displayMessage, powerUpEvent.messageDuration));
        }
    }

    IEnumerator ShowMessage(string message, float duration)
    {
        messageText.text = message;
        messageText.gameObject.SetActive(true);

        yield return new WaitForSeconds(duration);

        messageText.gameObject.SetActive(false);
        messageText.text = "";
    }
}