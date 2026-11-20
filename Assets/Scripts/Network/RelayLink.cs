// Pong — Unity Relay connection helper (Homework 3, Notes 6.1).
// Authenticates anonymously, requests Relay allocations, retrieves 6-letter join codes,
// and configures FishyUnityTransport before establishing the network connection.
using System;
using System.Threading.Tasks;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public static class RelayLink
{
    private static bool _initialized;

    public static async Task InitAsync()
    {
        if (_initialized) return;
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        _initialized = true;
    }

    public static async Task<string> StartRelayHost(int maxPlayers = 2)
    {
        await InitAsync();

        // 1. Ask Relay for an allocation
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);

        // 2. Ask Relay for a 6-letter join code
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        // 3. Configure FishyUnityTransport BEFORE starting connection
        FishyUnityTransport fut = InstanceFinder.NetworkManager.GetComponent<FishyUnityTransport>();
        if (fut == null) fut = InstanceFinder.NetworkManager.GetComponentInChildren<FishyUnityTransport>();
        if (fut != null)
        {
            fut.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );
        }

        // 4. Start host (server + client)
        InstanceFinder.ServerManager.StartConnection();
        InstanceFinder.ClientManager.StartConnection();

        Debug.Log($"[Relay] Join code: {joinCode}");
        return joinCode;
    }

    public static async Task JoinRelay(string joinCode)
    {
        await InitAsync();

        // 1. Join the allocation using the join code
        JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

        // 2. Configure FishyUnityTransport BEFORE starting client
        FishyUnityTransport fut = InstanceFinder.NetworkManager.GetComponent<FishyUnityTransport>();
        if (fut == null) fut = InstanceFinder.NetworkManager.GetComponentInChildren<FishyUnityTransport>();
        if (fut != null)
        {
            fut.SetRelayServerData(
                joinAlloc.RelayServer.IpV4,
                (ushort)joinAlloc.RelayServer.Port,
                joinAlloc.AllocationIdBytes,
                joinAlloc.Key,
                joinAlloc.ConnectionData,
                joinAlloc.HostConnectionData
            );
        }

        // 3. Start client
        InstanceFinder.ClientManager.StartConnection();
    }
}
