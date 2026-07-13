using UnityEngine;
using System.Collections.Generic;

public class KillMarkerPool : MonoBehaviour
{
    public static KillMarkerPool Instance { get; private set; }

    [Header("Pool Settings")]
    public GameObject killMarkerPrefab;
    public int poolSize = 20;

    Queue<GameObject> pool = new Queue<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < poolSize; i++)
        {
            GameObject marker = Instantiate(killMarkerPrefab, Vector3.zero, Quaternion.identity);
            DontDestroyOnLoad(marker);
            marker.SetActive(false);
            pool.Enqueue(marker);
        }
    }

    public void Spawn(Vector3 position, int goldValue = 0, Quaternion? rotation = null)
    {
        if (pool.Count == 0)
        {
            Debug.LogWarning("[KillMarkerPool] Pool exhausted - consider raising poolSize.");
            return;
        }

        GameObject marker = pool.Dequeue();
        marker.transform.position = position;
        marker.transform.rotation = rotation ?? Quaternion.identity;

        KillMarkerEffect effect = marker.GetComponent<KillMarkerEffect>();
        effect?.Init(Return);

        marker.SetActive(true);
    }

    void Return(GameObject marker)
    {
        marker.SetActive(false);
        pool.Enqueue(marker);
    }
}