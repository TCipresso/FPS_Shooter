using UnityEngine;

public class RangedProjectile : MonoBehaviour
{
    public int damage = 15;
    public float speed = 20f;
    public float lifetime = 4f;
    public PlayerStats owner;

    float spawnTime;

    void OnEnable()
    {
        spawnTime = Time.time;
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;

        if (Time.time - spawnTime >= lifetime)
            gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!gameObject.activeSelf) return;

        PlayerStats ps = other.GetComponent<PlayerStats>();
        if (ps != null)
        {
            ps.TakeDamage(damage);
            gameObject.SetActive(false);
            return;
        }

        if (other.GetComponent<ZombieBase>() != null) return;
        if (other.isTrigger) return;

        gameObject.SetActive(false);
    }
}