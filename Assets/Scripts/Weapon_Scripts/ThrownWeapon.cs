using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ThrownWeapon : MonoBehaviour
{
    public float lifetime = 5f;

    private bool launched;

    public void Launch(Vector3 velocity, Vector3 spin, Transform owner)
    {
        if (launched)
            return;

        launched = true;
        Collider[] weaponColliders = GetComponentsInChildren<Collider>();
        Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
        foreach (Collider weaponCollider in weaponColliders)
        {
            foreach (Collider ownerCollider in ownerColliders)
                Physics.IgnoreCollision(weaponCollider, ownerCollider);
        }

        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = false;
        body.useGravity = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.AddForce(velocity, ForceMode.VelocityChange);
        body.AddTorque(spin, ForceMode.VelocityChange);
        Destroy(gameObject, Mathf.Max(0.1f, lifetime));
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!launched)
            return;

        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
