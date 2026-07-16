using UnityEngine;
using System.Collections.Generic;
public class StageSetup : MonoBehaviour
{
    [Header("Spawn Points")]
    public List<Transform> spawnPoints = new List<Transform>();
    [Header("Portal Spawn Points")]
    public List<Transform> portalSpawnPoints = new List<Transform>();
    [Header("Interactables")]
    [Range(0f, 1f)] public float interactableSpawnChance = 0.6f;
    public List<GameObject> interactables = new List<GameObject>();
    void Start()
    {
        DisableAllInteractables();
        RollInteractable();
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
            else Debug.LogWarning("[StageSetup] No GameObject tagged Player found.");
        }
        else Debug.LogWarning("[StageSetup] No PlayerTransSpot found in scene.");
        RoundManager.Instance.OnSceneReady(spawnPoints, portalSpawnPoints);
    }
    void DisableAllInteractables()
    {
        foreach (GameObject obj in interactables)
            if (obj != null) obj.SetActive(false);
    }
    void RollInteractable()
    {
        if (interactables.Count == 0) return;
        if (Random.value > interactableSpawnChance) return;
        int index = Random.Range(0, interactables.Count);
        if (interactables[index] != null)
        {
            interactables[index].SetActive(true);
            Debug.Log($"[StageSetup] Enabled interactable: {interactables[index].name}");
        }
    }
}