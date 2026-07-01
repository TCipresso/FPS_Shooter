using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Pickup : MonoBehaviour
{
    [Header("Effect")]
    [SerializeField] private PickupEffectSO effect;

    [Header("Trigger Filter")]
    [SerializeField] private string playerTag = "Player";

    [Header("Behaviour")]
    [SerializeField] private bool destroyOnPickup = true;
    [SerializeField] private float destroyDelay = 0f;

    [Header("Spin")]
    [SerializeField] private bool spin = true;
    [SerializeField] private float spinSpeed = 90f;
    [SerializeField] private Vector3 spinAxis = Vector3.up;

    [Header("Bob")]
    [SerializeField] private bool bob = true;
    [SerializeField] private float bobHeight = 0.25f;
    [SerializeField] private float bobSpeed = 2f;

    private bool _consumed;
    private Vector3 _startLocalPos;

    private void Awake()
    {
        _startLocalPos = transform.localPosition;
    }

    private void Update()
    {
        if (_consumed) return;

        if (spin)
            transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.Self);

        if (bob)
        {
            float offset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = _startLocalPos + Vector3.up * offset;
        }
    }

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_consumed) return;
        if (!other.CompareTag(playerTag)) return;
        if (effect == null)
        {
            Debug.LogWarning($"Pickup on {name} has no PickupEffectSO assigned.", this);
            return;
        }

        _consumed = true;

        effect.Apply(other.gameObject);

        if (effect.pickupSFX != null)
            AudioSource.PlayClipAtPoint(effect.pickupSFX, transform.position);

        if (effect.pickupVFXPrefab != null)
            Instantiate(effect.pickupVFXPrefab, transform.position, Quaternion.identity);

        if (destroyOnPickup)
        {
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers) r.enabled = false;
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Destroy(gameObject, destroyDelay);
        }
    }
}