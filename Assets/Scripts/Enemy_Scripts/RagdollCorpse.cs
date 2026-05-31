using UnityEngine;
using System;

public class RagdollCorpse : MonoBehaviour
{
    public float destroyDelay = 4f;
    public Transform hipsJoint;

    public void Launch(Vector3 hitDirection, float force, string hitBoneName, Action onComplete)
    {
        Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody body in bodies)
        {
            if (body.name == hitBoneName)
                body.AddForce(hitDirection * force + Vector3.up * (force * 0.4f), ForceMode.Impulse);
            else
                body.AddForce(hitDirection * (force * 0.3f), ForceMode.Impulse);
        }

        StartCoroutine(ReturnAfterDelay(destroyDelay, onComplete));
    }

    System.Collections.IEnumerator ReturnAfterDelay(float delay, Action onComplete)
    {
        yield return new WaitForSeconds(delay);
        onComplete?.Invoke();
    }
}