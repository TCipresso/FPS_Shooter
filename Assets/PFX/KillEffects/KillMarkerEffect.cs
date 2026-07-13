using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ParticleSystem))]
public class KillMarkerEffect : MonoBehaviour
{
    [Tooltip("Assign here and uncheck 'Play On Awake' on the AudioSource itself. Play On Awake only fires once per pooled instance (on its original Instantiate), not on every SetActive(true) reuse.")]
    public AudioSource audioSource;

    [Tooltip("Safety net: forces return to the pool after this many seconds even if OnParticleSystemStopped never fires (e.g. Looping left on, or a child ParticleSystem missing its own Stop Action). Set comfortably above the effect's real duration.")]
    public float failsafeDuration = 3f;

    System.Action<GameObject> returnToPool;
    Coroutine failsafeRoutine;
    bool returned;

    public void Init(System.Action<GameObject> returnCallback)
    {
        returnToPool = returnCallback;
    }

    void OnEnable()
    {
        returned = false;
        audioSource?.Play();

        if (failsafeRoutine != null) StopCoroutine(failsafeRoutine);
        failsafeRoutine = StartCoroutine(FailsafeReturn());
    }

    IEnumerator FailsafeReturn()
    {
        yield return new WaitForSeconds(failsafeDuration);
        if (!returned)
        {
            Debug.LogWarning($"[{gameObject.name}] KillMarkerEffect failsafe triggered - OnParticleSystemStopped never fired. Check Looping is off and Stop Action = Callback on every ParticleSystem in this prefab.");
            ReturnToPoolOnce();
        }
    }

    // Called automatically by Unity when the ParticleSystem's Stop Action
    // (set in the Inspector on the ParticleSystem's Main module) is set to Callback.
    void OnParticleSystemStopped()
    {
        ReturnToPoolOnce();
    }

    void ReturnToPoolOnce()
    {
        if (returned) return;
        returned = true;

        if (failsafeRoutine != null)
        {
            StopCoroutine(failsafeRoutine);
            failsafeRoutine = null;
        }

        returnToPool?.Invoke(gameObject);
    }
}