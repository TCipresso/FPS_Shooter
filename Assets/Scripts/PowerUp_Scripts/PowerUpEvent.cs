using UnityEngine;

[CreateAssetMenu(fileName = "NewPowerUpEvent", menuName = "PowerUps/PowerUpEvent")]
public class PowerUpEvent : ScriptableObject
{
    [Header("Message")]
    public string displayMessage = "Power Up!";
    public float messageDuration = 3f;

    [Header("Audio")]
    public AudioClip activationSound;
    public AudioClip musicClip;
}