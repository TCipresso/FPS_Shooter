using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ZManager : MonoBehaviour
{
    public static ZManager Instance;

    public GameObject zPrefab;
    public int spawnCount = 50;
    public float spawnRadius = 20f;
    public float playerCheckInterval = 0.5f;

    Transform player;

    Queue<ZBase> pool = new Queue<ZBase>();
    List<ZBase> active = new List<ZBase>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject go = Instantiate(zPrefab, transform);
            ZBase z = go.GetComponent<ZBase>();
            go.SetActive(false);
            pool.Enqueue(z);
        }

        StartCoroutine(WaitForPlayer());
    }

    IEnumerator WaitForPlayer()
    {
        PlayerStats stats = null;

        while (stats == null)
        {
            stats = FindFirstObjectByType<PlayerStats>();
            if (stats == null)
                yield return new WaitForSeconds(playerCheckInterval);
        }

        player = stats.transform;

        for (int i = 0; i < spawnCount; i++)
            Spawn();
    }

    void Spawn()
    {
        if (pool.Count == 0) return;

        ZBase z = pool.Dequeue();
        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = player.position + new Vector3(offset.x, 0f, offset.y);

        z.transform.position = spawnPos;
        z.gameObject.SetActive(true);
        z.Init(player);
        active.Add(z);
    }

    public void Kill(ZBase z)
    {
        active.Remove(z);
        z.gameObject.SetActive(false);
        pool.Enqueue(z);
    }
}