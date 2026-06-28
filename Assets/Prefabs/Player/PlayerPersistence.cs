using UnityEngine;

public class PlayerPersistence : MonoBehaviour
{
    void Awake()
    {
        PlayerPersistence[] existing = FindObjectsByType<PlayerPersistence>(FindObjectsSortMode.None);
        if (existing.Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }
}