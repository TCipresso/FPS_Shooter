using UnityEngine;
using System;

public class RagdollCorpse : MonoBehaviour
{
    public float destroyDelay = 4f;
    public Transform hipsJoint;

    public void Launch(Vector3 hitDirection, float force, Action onComplete)
    {
        if (hipsJoint != null)
        {
            Rigidbody hipsRb = hipsJoint.GetComponent<Rigidbody>();
            if (hipsRb != null)
                hipsRb.AddForce(hitDirection * force + Vector3.up * (force * 0.5f), ForceMode.Impulse);
        }

        StartCoroutine(ReturnAfterDelay(destroyDelay, onComplete));
    }

    System.Collections.IEnumerator ReturnAfterDelay(float delay, Action onComplete)
    {
        yield return new WaitForSeconds(delay);
        onComplete?.Invoke();
    }
}