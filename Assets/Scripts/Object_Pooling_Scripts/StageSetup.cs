using UnityEngine;
using System.Collections.Generic;

public class StageSetup : MonoBehaviour
{
    public List<Transform> spawnPoints = new List<Transform>();

    void Start()
    {
        if (RoundManager.Instance == null) return;

        GameObject spot = GameObject.Find("PlayerTransSpot");
        if (spot != null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                player.transform.position = spot.transform.position;
                player.transform.rotation = spot.transform.rotation;
            }
            else
            {
                Debug.LogWarning("[StageSetup] No GameObject tagged Player found.");
            }
        }
        else
        {
            Debug.LogWarning("[StageSetup] No PlayerTransSpot found in scene.");
        }

        RoundManager.Instance.OnSceneReady(spawnPoints);
    }
}