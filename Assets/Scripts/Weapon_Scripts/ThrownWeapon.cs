using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ThrownWeapon : MonoBehaviour
{
    public float lifetime = 5f;
    public Vector3 rotationOffset;
    public Vector3 spinAxis = Vector3.right;

    private bool launched;

    public void Launch(Vector3 velocity, float spinSpeed, Transform owner, Quaternion aimRotation)
    {
        if (launched)
            return;

        launched = true;
        Vector3 spin = aimRotation * spinAxis.normalized * spinSpeed;
        Collider[] weaponColliders = GetComponentsInChildren<Collider>();
        Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
        foreach (Collider weaponCollider in weaponColliders)
        {
            foreach (Collider ownerCollider in ownerColliders)
                Physics.IgnoreCollision(weaponCollider, ownerCollider);
        }

        Rigidbody body = GetComponent<Rigidbody>();
        body.position = transform.position;
        body.rotation = transform.rotation;
        body.isKinematic = false;
        body.useGravity = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.AddForce(velocity, ForceMode.VelocityChange);
        body.maxAngularVelocity = Mathf.Max(body.maxAngularVelocity, Mathf.Abs(spinSpeed));
        body.angularVelocity = spin;
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
