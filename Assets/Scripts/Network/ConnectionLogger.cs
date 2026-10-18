using UnityEngine;
using FishNet;
using FishNet.Transporting;
using FishNet.Connection;

/// <summary>
/// Homework 1, Step 3: Logs server and client connection state transitions.
/// Attached to NetworkManager.
/// </summary>
public class ConnectionLogger : MonoBehaviour
{
    private void Start()
    {
        // Subscribed in Start (Awake is too early for FishNet network managers)
        if (InstanceFinder.NetworkManager == null) return;

        InstanceFinder.ServerManager.OnServerConnectionState += HandleServerConnectionState;
        InstanceFinder.ServerManager.OnRemoteConnectionState += HandleRemoteConnectionState;
        InstanceFinder.ClientManager.OnClientConnectionState += HandleClientConnectionState;
    }

    private void OnDestroy()
    {
        if (InstanceFinder.NetworkManager == null) return;

        if (InstanceFinder.ServerManager != null)
        {
            InstanceFinder.ServerManager.OnServerConnectionState -= HandleServerConnectionState;
            InstanceFinder.ServerManager.OnRemoteConnectionState -= HandleRemoteConnectionState;
        }

        if (InstanceFinder.ClientManager != null)
        {
            InstanceFinder.ClientManager.OnClientConnectionState -= HandleClientConnectionState;
        }
    }

    private void HandleServerConnectionState(ServerConnectionStateArgs args)
    {
        Debug.Log($"[Server] Connection state changed: {args.ConnectionState}");
    }

    private void HandleRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        int count = InstanceFinder.ServerManager.Clients.Count;
        Debug.Log($"[Server] Remote client {conn.ClientId} state: {args.ConnectionState}. Total clients: {count}");
    }

    private void HandleClientConnectionState(ClientConnectionStateArgs args)
    {
        Debug.Log($"[Client] Connection state changed: {args.ConnectionState}");
    }
}
