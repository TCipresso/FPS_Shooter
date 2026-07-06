using UnityEngine;
using Mirror;

public class SpawnDebug : NetworkBehaviour
{
    void OnDestroy()
    {
        Debug.Log($"PLAYER DESTROYED netId={netId}\n{System.Environment.StackTrace}");
    }
}