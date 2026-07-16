using System.Collections.Generic;
using UnityEngine;
public class TokenVendingMachine : MonoBehaviour
{
    [Header("Token Pool")]
    public List<GameObject> tokenPrefabPool;

    [Header("Slots")]
    public List<Transform> slotPoints;

    [Header("Roll Settings")]
    public int minTokens = 1;
    public int maxTokens = 3;

    List<GameObject> spawnedInstances = new List<GameObject>();

    void OnEnable()
    {
        RerollTokens();
    }

    public void RerollTokens()
    {
        ClearSpawned();

        int slotCount = Mathf.Min(slotPoints.Count, tokenPrefabPool.Count);
        int count = Mathf.Min(Random.Range(minTokens, maxTokens + 1), slotCount);

        List<GameObject> pool = new List<GameObject>(tokenPrefabPool);

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, pool.Count);
            GameObject prefab = pool[index];
            pool.RemoveAt(index);

            GameObject instance = Instantiate(prefab, slotPoints[i].position, slotPoints[i].rotation, slotPoints[i]);
            spawnedInstances.Add(instance);
        }
    }

    void ClearSpawned()
    {
        foreach (GameObject go in spawnedInstances)
        {
            if (go != null) Destroy(go);
        }
        spawnedInstances.Clear();
    }
}