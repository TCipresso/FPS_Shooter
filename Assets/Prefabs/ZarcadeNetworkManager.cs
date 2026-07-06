using UnityEngine;
using Mirror;

public class ZarcadeNetworkManager : NetworkManager
{
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        Debug.Log($"SERVER: connection {conn.connectionId} connected");
    }

    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);
        Debug.Log($"SERVER: connection {conn.connectionId} is READY");
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);
        Debug.Log($"SERVER: player spawned for connection {conn.connectionId}");
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.Log($"SERVER: connection {conn.connectionId} DISCONNECTED");
        base.OnServerDisconnect(conn);
    }
}