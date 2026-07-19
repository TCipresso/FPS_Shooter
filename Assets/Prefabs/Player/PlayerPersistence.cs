using UnityEngine;
using Mirror;

public class PlayerPersistence : NetworkBehaviour
{
    public override void OnStartLocalPlayer()
    {
        DontDestroyOnLoad(gameObject);
    }
}