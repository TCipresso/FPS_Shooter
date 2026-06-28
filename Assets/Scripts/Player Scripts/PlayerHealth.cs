using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int hitsToDown = 2;

    [Header("Lives")]
    public int lives = 3;

    [Header("Hit Recovery")]
    public float hitRecoveryDelay = 2f;

    [Header("Down Settings")]
    public float downDuration = 3f;

    [Header("Hit Indicator")]
    public CanvasGroup hitIndicator;
    public float hitFlashMax = 0.6f;
    public float hitFlashOutSpeed = 3f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hitSound;
    public AudioClip downSound;

    [Header("Events")]
    public UnityEvent onHit;
    public UnityEvent onDown;
    public UnityEvent onRevive;
    public UnityEvent onGameOver;

    public bool IsDown { get; private set; }
    public bool IsGameOver { get; private set; }
    public int HitsRemaining => hitsRemaining;
    public float HitRecoveryProgress => hitsRemaining < hitsToDown ? hitRecoveryTimer / hitRecoveryDelay : 0f;

    int hitsRemaining;
    float hitRecoveryTimer = 0f;

    void Start()
    {
        hitsRemaining = hitsToDown;
        if (hitIndicator != null) hitIndicator.alpha = 0f;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (IsDown || IsGameOver) return;
        if (hitsRemaining >= hitsToDown) return;

        hitRecoveryTimer += Time.deltaTime;
        if (hitRecoveryTimer >= hitRecoveryDelay)
        {
            hitsRemaining = Mathf.Min(hitsRemaining + 1, hitsToDown);
            hitRecoveryTimer = 0f;
            Debug.Log($"[PlayerHealth] Recovered a hit. Hits: {hitsRemaining}/{hitsToDown}");
        }
    }

    public void TakeHit()
    {
        if (IsDown || IsGameOver) return;

        hitsRemaining--;
        hitRecoveryTimer = 0f;
        FlashHitIndicator();
        PlaySound(hitSound);
        onHit.Invoke();

        if (hitsRemaining <= 0)
            Down();
    }

    void FlashHitIndicator()
    {
        if (hitIndicator == null) return;
        StopCoroutine(nameof(HitFlashRoutine));
        StartCoroutine(nameof(HitFlashRoutine));
    }

    IEnumerator HitFlashRoutine()
    {
        hitIndicator.alpha = hitFlashMax;
        while (hitIndicator.alpha > 0f)
        {
            hitIndicator.alpha = Mathf.MoveTowards(hitIndicator.alpha, 0f, hitFlashOutSpeed * Time.deltaTime);
            yield return null;
        }
        hitIndicator.alpha = 0f;
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip);
    }

    void Down()
    {
        IsDown = true;
        lives--;
        PlaySound(downSound);
        onDown.Invoke();
        Debug.Log($"[PlayerHealth] Player is down. Lives remaining: {lives}");

        if (lives <= 0)
            GameOver();
        else
            StartCoroutine(DownRoutine());
    }

    IEnumerator DownRoutine()
    {
        yield return new WaitForSeconds(downDuration);
        Revive();
    }

    void GameOver()
    {
        IsGameOver = true;
        onGameOver.Invoke();
        Debug.Log("[PlayerHealth] Game Over.");
    }

    public void Revive()
    {
        if (IsGameOver) return;
        IsDown = false;
        hitsRemaining = hitsToDown;
        hitRecoveryTimer = 0f;
        onRevive.Invoke();
        Debug.Log($"[PlayerHealth] Player revived. Lives remaining: {lives}");
    }

    public void AddLife(int amount = 1)
    {
        lives += amount;
        Debug.Log($"[PlayerHealth] Lives: {lives}");
    }

    public void AddHit(int amount = 1)
    {
        hitsToDown += amount;
        hitsRemaining += amount;
        Debug.Log($"[PlayerHealth] Hits to down: {hitsToDown}");
    }
}