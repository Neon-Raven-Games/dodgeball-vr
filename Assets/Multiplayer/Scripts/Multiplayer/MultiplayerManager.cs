using System;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

public class MultiplayerManager : MonoBehaviour
{
    private NetworkManager _networkManager;
    [SerializeField] private NetworkObject serverOwnershipManager;
    [SerializeField] private NetworkObject netBallController;
    [SerializeField] private NetworkObject netTeamController;
    [SerializeField] private ushort serverConnectionPort;
    [SerializeField] private ushort internalServerPort;
    [SerializeField] private string serverAddress;

    public void ResetBalls() => NetBallController.ResetBalls();

    public void StartServerGameAsHost()
    {
#if UNITY_SERVER
        try
        {
            Debug.Log($"Setting server port: {internalServerPort}");
            
            _networkManager.TransportManager.Transport.SetPort(internalServerPort);
            _networkManager.ServerManager.StartConnection(internalServerPort);

            _networkManager.ServerManager.OnServerConnectionState += ConnectOnServerStart;
            _networkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
            Debug.Log("Starting Server Connection");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception: {ex.Message}");
        }
#endif
    }

    private void OnRemoteConnectionState(NetworkConnection arg1, RemoteConnectionStateArgs arg2)
    {
        Debug.Log("Remote Connection State: " + arg2.ConnectionState);
        Debug.Log("Transport index of remote connect state change: " + arg2.TransportIndex);
    }

    private void ConnectOnServerStart(ServerConnectionStateArgs args)
    {
        Debug.Log(args.ConnectionState);
        if (args.ConnectionState != LocalConnectionState.Started) return;

        _networkManager.ServerManager.OnServerConnectionState -= ConnectOnServerStart;
        Debug.Log("Server Started Successfully");
        InitializeBalls();
        SetUpOwnership();
        SetUpTeams();
        NeonRavenBroadcast.Initialize();
        Debug.Log("Spawned Dodgeball Controller, Balls, and Ownership Manager on Server");
    }

    private void SetUpTeams()
    {
        var teamController =
            _networkManager.GetPooledInstantiated(netTeamController, Vector3.zero, Quaternion.identity, true);
        _networkManager.ServerManager.Spawn(teamController);
    }
    
    private void SetUpOwnership()
    {
        var ownership =
            _networkManager.GetPooledInstantiated(serverOwnershipManager, Vector3.zero, Quaternion.identity, true);
        _networkManager.ServerManager.Spawn(ownership);
    }

    private void InitializeBalls()
    {
        var ballController =
            _networkManager.GetPooledInstantiated(netBallController, Vector3.zero, Quaternion.identity, true);
        _networkManager.ServerManager.Spawn(ballController);

        // for (var i = 0; i < 3; i++) NetBallController.SpawnBallWithIndex(i);
        NetBallController.SpawnBallWithIndex(1);
    }

    public void StartClientGame()
    {
        Debug.Log($"Connecting to server at {serverAddress}:{serverConnectionPort}");
        try
        {
            var connected = _networkManager.ClientManager.StartConnection(serverAddress, serverConnectionPort);
            Debug.Log($"Client connection initiated: {connected}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error while starting client connection: {ex.Message}");
        }
    }

    private void OnConnectedClients(ConnectedClientsArgs obj)
    {
        Debug.Log("Client Connected" + obj.ClientIds);
    }

    [SerializeField] private string webSocketUrl = "ws://localhost/ws";
    [SerializeField] private bool useClientDiagnostics = true;

    private NetworkDiagnosticConnection _networkDiagnosticConnection;

    private void Start()
    {
        _networkManager = GetComponent<NetworkManager>();
        Debug.Log(_networkManager.TransportManager.Transport.GetServerBindAddress(IPAddressType.IPv4));
        if (useClientDiagnostics)
            _networkDiagnosticConnection = new NetworkDiagnosticConnection(webSocketUrl, _networkManager);
#if UNITY_SERVER
        StartServerGameAsHost();
        _networkManager.ClientManager.OnConnectedClients += OnConnectedClients;
#endif
    }

    private void OnDisable()
    {
        if (useClientDiagnostics) _networkDiagnosticConnection.Close();
    }
}

public static class Utils
{
    public static Vector3 GetRandomSpawnPosition()
    {
        // This is where we will implement our spawn logic
        // the team will provide default rotation for characters, based on their team.
        // we want to create a formation along the line of the team's side of the court
        return new Vector3(UnityEngine.Random.Range(-5f, 5f), 1f, UnityEngine.Random.Range(-5f, 5f));
    }
}