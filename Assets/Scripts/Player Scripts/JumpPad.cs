using UnityEngine;

[RequireComponent(typeof(Collider))]
public class JumpPad : MonoBehaviour
{
    [SerializeField] private float launchUpSpeed = 20f;
    [SerializeField] private float launchForwardSpeed = 0f;
    [SerializeField] private Transform launchDirection;
    [SerializeField] private float retriggerDelay = 0.25f;
    [SerializeField] private float maxHorizontalSpeed = 18f; // set to -1 for no cap
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip launchSound;

    private float cooldownTimer;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (cooldownTimer > 0f) return;

        PlayerFpsController player = other.GetComponent<PlayerFpsController>();
        if (player == null) return;

        Vector3 dir = launchDirection != null ? launchDirection.forward : transform.up;
        Vector3 velocity = dir.normalized * launchForwardSpeed + Vector3.up * launchUpSpeed;

        // overrideHorizontal: false keeps whatever momentum you carried in (slide/dash speed),
        // only the vertical pop is guaranteed consistent every time.
        player.Launch(velocity, overrideHorizontal: false, maxHorizontalSpeed: maxHorizontalSpeed);
        cooldownTimer = retriggerDelay;

        if (audioSource != null && launchSound != null)
            audioSource.PlayOneShot(launchSound);
    }
}